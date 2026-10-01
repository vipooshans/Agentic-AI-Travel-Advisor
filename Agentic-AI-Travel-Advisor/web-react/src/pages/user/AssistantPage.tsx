import { useEffect, useRef, useState, type FormEvent } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { errorMessage } from '../../api/client';
import { bookingStatusLabels, recommendationTypeLabels } from '../../api/enums';
import { aiApi, settingsApi } from '../../api/services';
import type { Booking, BookingProposal, ChatMessage, ChatResponse, ChatStatus, ToolCallSummary, TravelPlan } from '../../api/types';
import { Alert, Badge, Spinner } from '../../components/ui';
import { formatDate, formatDateTime, formatMoney } from '../../lib/format';
import { useAsync } from '../../lib/useAsync';
import { useNow } from '../../lib/useNow';
import { PlanView } from './PlanView';

interface ThreadMessage {
  role: 'user' | 'assistant';
  content: string;
  status?: ChatStatus | null;
  plan?: TravelPlan | null;
  pendingBooking?: BookingProposal | null;
  booking?: Booking | null;
  bookingId?: number | null;
  mode?: string;
  agents?: string[];
  toolCalls?: ToolCallSummary[];
}

const SUGGESTIONS = [
  'Plan a 3-day trip to Ella for 2 people with a budget of LKR 100,000',
  'Find a hotel in Kandy for 2 nights next weekend under LKR 30,000',
  'Suggest a hiking package for 4 travelers',
];

const statusTone: Partial<Record<ChatStatus, 'success' | 'warning' | 'danger' | 'info'>> = {
  plan: 'success',
  booking_created: 'success',
  booking_proposal: 'info',
  clarification: 'warning',
  over_budget: 'danger',
  no_match: 'warning',
  booking_failed: 'danger',
  refused: 'danger',
};

const fromHistory = (m: ChatMessage): ThreadMessage => ({ ...m, role: m.role === 'user' ? 'user' : 'assistant' });

const fromResponse = (r: ChatResponse): ThreadMessage => ({
  role: 'assistant',
  content: r.message,
  status: r.status,
  plan: r.plan,
  pendingBooking: r.pendingBooking,
  booking: r.booking,
  bookingId: r.booking?.id,
  mode: r.mode,
  agents: r.agents,
  toolCalls: r.toolCalls,
});

export function AssistantPage() {
  const [params, setParams] = useSearchParams();
  const urlId = params.get('c') ? Number(params.get('c')) : undefined;
  const settings = useAsync(() => settingsApi.public(), []);
  const conversations = useAsync(() => aiApi.conversations(), []);
  const recommendations = useAsync(() => aiApi.recommendations(), []);

  const [conversationId, setConversationId] = useState<number | undefined>(urlId);
  const [thread, setThread] = useState<ThreadMessage[]>([]);
  const [historyError, setHistoryError] = useState<string | null>(null);
  const [input, setInput] = useState('');
  const [sending, setSending] = useState(false);
  const [sendError, setSendError] = useState<string | null>(null);
  const endRef = useRef<HTMLDivElement>(null);
  // Set when the first reply of a new chat creates a conversation: the thread is already on screen,
  // so the URL update that follows must not reload (and briefly blank) it.
  const createdId = useRef<number | undefined>(undefined);

  useEffect(() => {
    if (urlId !== undefined && urlId === createdId.current) {
      createdId.current = undefined;
      return;
    }
    let active = true;
    const load = urlId ? aiApi.conversation(urlId).then((c) => c.messages.map(fromHistory)) : Promise.resolve<ThreadMessage[]>([]);
    load.then(
      (messages) => {
        if (!active) return;
        setThread(messages);
        setConversationId(urlId);
        setHistoryError(null);
      },
      (e: unknown) => {
        if (!active) return;
        setThread([]);
        setConversationId(undefined);
        setHistoryError(errorMessage(e));
      },
    );
    return () => {
      active = false;
    };
  }, [urlId]);

  useEffect(() => {
    endRef.current?.scrollIntoView?.({ behavior: 'smooth', block: 'end' });
  }, [thread.length, sending]);

  async function send(message: string, confirmBookingId?: string) {
    const text = message.trim();
    if (!text || sending) return;
    setSending(true);
    setSendError(null);
    setThread((t) => [...t, { role: 'user', content: text }]);
    setInput('');
    try {
      const response = await aiApi.chat({ conversationId, message: text, confirmBookingId });
      setThread((t) => [...t, fromResponse(response)]);
      if (response.conversationId !== conversationId) {
        createdId.current = response.conversationId;
        setConversationId(response.conversationId);
        setParams({ c: String(response.conversationId) }, { replace: true });
        conversations.reload();
      }
      if (response.plan || response.status === 'booking_created') recommendations.reload();
    } catch (e) {
      setSendError(errorMessage(e));
    } finally {
      setSending(false);
    }
  }

  function submit(e: FormEvent) {
    e.preventDefault();
    void send(input);
  }

  const lastAssistant = [...thread].reverse().find((m) => m.role === 'assistant');
  const disabled = settings.data?.aiAssistantEnabled === false;

  return (
    <div className="assistant">
      <aside className="assistant-side">
        <button className="btn btn-primary btn-block" onClick={() => setParams({})} disabled={!conversationId && thread.length === 0}>
          + New conversation
        </button>
        <h3>Conversations</h3>
        {conversations.error && <Alert>{conversations.error}</Alert>}
        <ul className="conversation-list">
          {conversations.data?.map((c) => (
            <li key={c.id}>
              <button className={c.id === conversationId ? 'active' : ''} onClick={() => setParams({ c: String(c.id) })}>
                <span>{c.title || `Conversation ${c.id}`}</span>
                <small>{formatDateTime(c.updatedAt)}</small>
              </button>
            </li>
          ))}
          {conversations.data?.length === 0 && <li className="muted small">No conversations yet.</li>}
        </ul>
        <h3>Recent recommendations</h3>
        {recommendations.error && <Alert>{recommendations.error}</Alert>}
        <ul className="plain-list small">
          {recommendations.data?.slice(0, 6).map((r) => (
            <li key={r.id}>
              <Badge>{recommendationTypeLabels[r.itemType]}</Badge>{' '}
              {r.hotelId ? (
                <Link to={`/hotels/${r.hotelId}`}>{r.title}</Link>
              ) : r.travelPackageId ? (
                <Link to={`/packages/${r.travelPackageId}`}>{r.title}</Link>
              ) : (
                r.title
              )}{' '}
              <span className="muted">{formatMoney(r.estimatedCost)}</span>
            </li>
          ))}
          {recommendations.data?.length === 0 && <li className="muted">Nothing recommended yet.</li>}
        </ul>
      </aside>

      <section className="assistant-main" aria-label="Chat">
        {disabled && <Alert kind="warning">The AI travel assistant is currently turned off by the administrator.</Alert>}
        {historyError && <Alert>{historyError}</Alert>}

        <div className="thread" aria-live="polite">
          {thread.length === 0 && !historyError && (
            <div className="thread-empty">
              <h2>Where would you like to go?</h2>
              <p className="muted">
                Tell me the destination, dates or trip length, number of travelers and your budget. I only use live hotels, packages and
                transport from this platform, and I never book anything without your confirmation.
              </p>
              <div className="suggestions">
                {SUGGESTIONS.map((s) => (
                  <button key={s} className="chip" onClick={() => send(s)} disabled={sending || disabled}>
                    {s}
                  </button>
                ))}
              </div>
            </div>
          )}

          {thread.map((m, i) => (
            <MessageBubble
              key={i}
              message={m}
              canConfirm={m === lastAssistant && !sending && !disabled}
              onConfirm={(p) => send(`Confirm booking: ${p.title}`, p.id)}
            />
          ))}
          {sending && (
            <div className="bubble bubble-assistant">
              <Spinner label="Planning with live availability…" />
            </div>
          )}
          <div ref={endRef} />
        </div>

        {sendError && <Alert onClose={() => setSendError(null)}>{sendError}</Alert>}

        <form className="composer" onSubmit={submit}>
          <textarea
            aria-label="Message"
            rows={2}
            maxLength={2000}
            placeholder="e.g. 4 days in Ella for 2 people, budget LKR 120,000, we love hiking"
            value={input}
            disabled={disabled}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                void send(input);
              }
            }}
          />
          <button className="btn btn-primary" type="submit" disabled={sending || disabled || !input.trim()}>
            Send
          </button>
        </form>
      </section>
    </div>
  );
}

function MessageBubble({
  message: m,
  canConfirm,
  onConfirm,
}: {
  message: ThreadMessage;
  canConfirm: boolean;
  onConfirm: (p: BookingProposal) => void;
}) {
  if (m.role === 'user') {
    return <div className="bubble bubble-user">{m.content}</div>;
  }
  const tone = m.status ? statusTone[m.status] : undefined;
  return (
    <div className="bubble bubble-assistant">
      {m.status && m.status !== 'info' && <Badge tone={tone ?? 'neutral'}>{m.status.replace('_', ' ')}</Badge>}
      <p className="bubble-text">{m.content}</p>
      {m.plan && <PlanView plan={m.plan} />}
      {m.pendingBooking && <ProposalCard proposal={m.pendingBooking} canConfirm={canConfirm} onConfirm={onConfirm} />}
      {m.booking && (
        <div className="proposal proposal-done">
          <strong>Booking #{m.booking.id}</strong> · status <strong>{bookingStatusLabels[m.booking.status]}</strong> ·{' '}
          {formatMoney(m.booking.totalPrice)}
          <p className="small muted">The provider still has to confirm it. <Link to="/bookings">View my bookings</Link></p>
        </div>
      )}
      {!m.booking && m.bookingId && (
        <p className="small">
          Booking #{m.bookingId} was created. <Link to="/bookings">View my bookings</Link>
        </p>
      )}
      {m.toolCalls && m.toolCalls.length > 0 && (
        <details className="tool-calls">
          <summary className="small muted">
            {m.mode === 'llm' ? 'Model-driven' : 'Rule-based'} · {m.toolCalls.length} tool call{m.toolCalls.length === 1 ? '' : 's'}
          </summary>
          <ul className="plain-list small">
            {m.toolCalls.map((t, i) => (
              <li key={i}>
                {t.success ? '✓' : '✗'} {t.name} <span className="muted">({t.durationMs} ms)</span>
                {t.error && <span className="muted"> — {t.error}</span>}
              </li>
            ))}
          </ul>
          {m.agents && m.agents.length > 0 && <p className="small muted">Agents: {m.agents.join(', ')}</p>}
        </details>
      )}
    </div>
  );
}

function ProposalCard({
  proposal: p,
  canConfirm,
  onConfirm,
}: {
  proposal: BookingProposal;
  canConfirm: boolean;
  onConfirm: (p: BookingProposal) => void;
}) {
  const now = useNow();
  const expired = Date.parse(p.expiresAt) <= now;
  return (
    <div className="proposal">
      <div>
        <strong>{p.title}</strong>
        <p className="small muted">
          {formatDate(p.checkIn)}
          {p.checkOut && ` → ${formatDate(p.checkOut)}`} · {p.guests} guest{p.guests === 1 ? '' : 's'} · quoted{' '}
          {formatMoney(p.quotedTotal, p.currency)}
        </p>
        <p className="small muted">Nothing is booked until you confirm. The quote expires {formatDateTime(p.expiresAt)}.</p>
      </div>
      {expired ? (
        <Badge>Expired</Badge>
      ) : canConfirm ? (
        <button className="btn btn-primary btn-sm" onClick={() => onConfirm(p)}>
          Confirm booking
        </button>
      ) : (
        <Badge>Superseded</Badge>
      )}
    </div>
  );
}
