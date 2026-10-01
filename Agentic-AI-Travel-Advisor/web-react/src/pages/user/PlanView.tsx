import { Link } from 'react-router-dom';
import { itinerariesApi } from '../../api/services';
import type { TravelPlan } from '../../api/types';
import { ActionFeedback, Badge } from '../../components/ui';
import { formatDate, formatMoney } from '../../lib/format';
import { planToItinerary } from '../../lib/plan';
import { useAction } from '../../lib/useAction';

export function PlanView({ plan, conversationId }: { plan: TravelPlan; conversationId?: number }) {
  const save = useAction();
  const hotels = plan.hotels;
  const selectedHotel = hotels.find((h) => h.selected);
  const selectedPackage = plan.travelPackages.find((p) => p.selected);
  const transport = plan.transportation.filter((t) => t.selected);

  return (
    <section className="plan" aria-label={`Trip plan for ${plan.destination}`}>
      <header className="plan-head">
        <div>
          <h3>
            {plan.destination}
            {plan.country && <span className="muted">, {plan.country}</span>}
          </h3>
          <p className="muted small">
            {formatDate(plan.startDate)} → {formatDate(plan.endDate)} · {plan.duration} days / {plan.nights} nights · {plan.travelers}{' '}
            traveler{plan.travelers === 1 ? '' : 's'}
          </p>
        </div>
        <Badge tone={plan.withinBudget ? 'success' : 'danger'}>{plan.withinBudget ? 'Within budget' : 'Over budget'}</Badge>
      </header>

      <div className="plan-costs">
        <div>
          <span className="muted small">Estimated total</span>
          <strong>{formatMoney(plan.estimatedTotal, plan.currency)}</strong>
        </div>
        <div>
          <span className="muted small">Budget</span>
          <strong>{formatMoney(plan.budget, plan.currency)}</strong>
        </div>
        <div>
          <span className="muted small">Stay</span>
          {formatMoney(plan.costBreakdown.accommodation, plan.currency)}
        </div>
        <div>
          <span className="muted small">Packages</span>
          {formatMoney(plan.costBreakdown.packages, plan.currency)}
        </div>
        <div>
          <span className="muted small">Transport</span>
          {formatMoney(plan.costBreakdown.transportation, plan.currency)}
        </div>
      </div>

      {selectedHotel && (
        <div className="plan-block">
          <h4>Stay</h4>
          <p>
            <Link to={`/hotels/${selectedHotel.hotelId}`}>{selectedHotel.name}</Link> · {selectedHotel.roomName} × {selectedHotel.rooms} ·{' '}
            {selectedHotel.nights} nights × {formatMoney(selectedHotel.pricePerNight, plan.currency)} ={' '}
            <strong>{formatMoney(selectedHotel.totalCost, plan.currency)}</strong>{' '}
            {selectedHotel.availabilityChecked ? <Badge tone="success">Availability checked</Badge> : <Badge>Not yet checked</Badge>}
          </p>
          {hotels.length > 1 && (
            <details>
              <summary className="small">{hotels.length - 1} alternative stay(s)</summary>
              <ul className="plain-list small">
                {hotels
                  .filter((h) => !h.selected)
                  .map((h) => (
                    <li key={`${h.hotelId}-${h.roomId}`}>
                      <Link to={`/hotels/${h.hotelId}`}>{h.name}</Link> · {h.roomName} · {formatMoney(h.totalCost, plan.currency)}
                    </li>
                  ))}
              </ul>
            </details>
          )}
        </div>
      )}

      {selectedPackage && (
        <div className="plan-block">
          <h4>Package</h4>
          <p>
            <Link to={`/packages/${selectedPackage.packageId}`}>{selectedPackage.title}</Link> · {selectedPackage.durationDays} days ·{' '}
            {formatMoney(selectedPackage.pricePerPerson, plan.currency)} pp = <strong>{formatMoney(selectedPackage.totalCost, plan.currency)}</strong>
            {selectedPackage.remainingPlaces != null && <span className="muted"> · {selectedPackage.remainingPlaces} places left</span>}
          </p>
        </div>
      )}

      {transport.length > 0 && (
        <div className="plan-block">
          <h4>Transport</h4>
          <ul className="plain-list">
            {transport.map((t) => (
              <li key={t.transportationId}>
                {t.mode}: {t.from} → {t.to}
                {t.departureTime && ` at ${t.departureTime}`} · {t.trips} trip{t.trips === 1 ? '' : 's'} ={' '}
                {formatMoney(t.totalCost, plan.currency)}
              </li>
            ))}
          </ul>
        </div>
      )}

      {plan.itinerary.length > 0 && (
        <div className="plan-block">
          <h4>Day by day</h4>
          <ol className="days">
            {plan.itinerary.map((day) => (
              <li key={day.day}>
                <strong>
                  Day {day.day} · {formatDate(day.date)}
                </strong>
                <ul className="plain-list small">
                  {day.items.map((item, i) => (
                    <li key={i}>
                      <span className="time">{item.time}</span> {item.title}
                      {item.description && <span className="muted"> — {item.description}</span>}
                    </li>
                  ))}
                </ul>
              </li>
            ))}
          </ol>
        </div>
      )}

      {plan.warnings.length > 0 && (
        <ul className="plan-notes warning">
          {plan.warnings.map((w, i) => (
            <li key={i}>{w}</li>
          ))}
        </ul>
      )}
      {plan.assumptions.length > 0 && (
        <details>
          <summary className="small muted">Assumptions</summary>
          <ul className="plan-notes">
            {plan.assumptions.map((a, i) => (
              <li key={i}>{a}</li>
            ))}
          </ul>
        </details>
      )}

      <div className="row-actions">
        <button
          type="button"
          className="btn btn-secondary btn-sm"
          disabled={save.busy || save.success !== null}
          onClick={() => save.run(() => itinerariesApi.create(planToItinerary(plan, conversationId)), 'Saved to your itineraries.')}
        >
          {save.success ? 'Saved' : 'Save as itinerary'}
        </button>
      </div>
      <ActionFeedback error={save.error} success={save.success} />
    </section>
  );
}
