import { useState, type FormEvent } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { ApprovalStatus } from '../../api/enums';
import { destinationsApi, hotelsApi, packagesApi } from '../../api/services';
import type { HotelSearchQuery, PackageSearchQuery } from '../../api/types';
import { useAuth } from '../../auth/authContext';
import { Alert, ApprovalBadge, EmptyState, Field, ListingImage, Spinner } from '../../components/ui';
import { formatMoney, formatRating, optionalNumber } from '../../lib/format';
import { useAsync } from '../../lib/useAsync';

type Tab = 'hotels' | 'packages';

export function HomePage() {
  const { user } = useAuth();
  const [params, setParams] = useSearchParams();
  const tab: Tab = params.get('tab') === 'packages' ? 'packages' : 'hotels';

  return (
    <>
      <section className="hero">
        <h1>Find your next stay or tour package</h1>
        <p>
          Search approved hotels and travel packages, or
          {user?.role === 'USER' ? (
            <>
              {' '}
              <Link to="/assistant">ask the AI assistant</Link> to plan a trip within your budget.
            </>
          ) : (
            ' sign in as a traveler to let the AI assistant plan a trip within your budget.'
          )}
        </p>
        <div className="tabs" role="tablist">
          <button role="tab" aria-selected={tab === 'hotels'} className={tab === 'hotels' ? 'active' : ''} onClick={() => setParams({ tab: 'hotels' })}>
            Hotels
          </button>
          <button
            role="tab"
            aria-selected={tab === 'packages'}
            className={tab === 'packages' ? 'active' : ''}
            onClick={() => setParams({ tab: 'packages' })}
          >
            Travel packages
          </button>
        </div>
      </section>
      {tab === 'hotels' ? <HotelSearch /> : <PackageSearch />}
    </>
  );
}

function HotelSearch() {
  const [draft, setDraft] = useState({ q: '', city: '', maxPrice: '', guests: '' });
  const [query, setQuery] = useState<HotelSearchQuery>({});
  const hotels = useAsync(() => hotelsApi.search(query), [query]);

  function submit(e: FormEvent) {
    e.preventDefault();
    setQuery({
      q: draft.q.trim() || undefined,
      city: draft.city.trim() || undefined,
      maxPrice: optionalNumber(draft.maxPrice),
      guests: optionalNumber(draft.guests),
    });
  }

  return (
    <section>
      <form className="card search-bar" onSubmit={submit} aria-label="Search hotels">
        <Field label="Keyword">
          <input placeholder="Name or description" value={draft.q} onChange={(e) => setDraft({ ...draft, q: e.target.value })} />
        </Field>
        <Field label="City">
          <input placeholder="e.g. Ella" value={draft.city} onChange={(e) => setDraft({ ...draft, city: e.target.value })} />
        </Field>
        <Field label="Max price / night">
          <input type="number" min={0} value={draft.maxPrice} onChange={(e) => setDraft({ ...draft, maxPrice: e.target.value })} />
        </Field>
        <Field label="Guests">
          <input type="number" min={1} value={draft.guests} onChange={(e) => setDraft({ ...draft, guests: e.target.value })} />
        </Field>
        <button className="btn btn-primary" type="submit">
          Search
        </button>
      </form>

      {hotels.error && <Alert>{hotels.error}</Alert>}
      {hotels.loading && !hotels.data ? (
        <Spinner label="Loading hotels…" />
      ) : hotels.data && hotels.data.length === 0 ? (
        <EmptyState title="No hotels match your search">Try a different city or a higher price limit.</EmptyState>
      ) : (
        <div className="card-grid">
          {hotels.data?.map((h) => (
            <Link key={h.id} to={`/hotels/${h.id}`} className="card listing">
              <ListingImage src={h.imageUrl} />
              <div className="listing-body">
                <div className="listing-title">
                  <h3>{h.name}</h3>
                  {h.approvalStatus !== ApprovalStatus.Approved && <ApprovalBadge status={h.approvalStatus} />}
                </div>
                <p className="muted">
                  {h.city}, {h.country}
                </p>
                <p className="muted small">{formatRating(h.averageRating, h.reviewCount)}</p>
                <p className="price">
                  {h.minPricePerNight != null ? (
                    <>
                      from <strong>{formatMoney(h.minPricePerNight)}</strong> / night
                    </>
                  ) : (
                    'No rooms listed'
                  )}
                </p>
              </div>
            </Link>
          ))}
        </div>
      )}
    </section>
  );
}

function PackageSearch() {
  const destinations = useAsync(() => destinationsApi.list(), []);
  const [draft, setDraft] = useState({ q: '', destinationId: '', maxPrice: '', maxDurationDays: '' });
  const [query, setQuery] = useState<PackageSearchQuery>({});
  const packages = useAsync(() => packagesApi.search(query), [query]);

  function submit(e: FormEvent) {
    e.preventDefault();
    setQuery({
      q: draft.q.trim() || undefined,
      destinationId: optionalNumber(draft.destinationId),
      maxPrice: optionalNumber(draft.maxPrice),
      maxDurationDays: optionalNumber(draft.maxDurationDays),
    });
  }

  return (
    <section>
      <form className="card search-bar" onSubmit={submit} aria-label="Search packages">
        <Field label="Keyword">
          <input placeholder="e.g. hiking" value={draft.q} onChange={(e) => setDraft({ ...draft, q: e.target.value })} />
        </Field>
        <Field label="Destination">
          <select value={draft.destinationId} onChange={(e) => setDraft({ ...draft, destinationId: e.target.value })}>
            <option value="">Any destination</option>
            {destinations.data?.map((d) => (
              <option key={d.id} value={d.id}>
                {d.name}, {d.country}
              </option>
            ))}
          </select>
        </Field>
        <Field label="Max price / person">
          <input type="number" min={0} value={draft.maxPrice} onChange={(e) => setDraft({ ...draft, maxPrice: e.target.value })} />
        </Field>
        <Field label="Max days">
          <input type="number" min={1} value={draft.maxDurationDays} onChange={(e) => setDraft({ ...draft, maxDurationDays: e.target.value })} />
        </Field>
        <button className="btn btn-primary" type="submit">
          Search
        </button>
      </form>

      {packages.error && <Alert>{packages.error}</Alert>}
      {packages.loading && !packages.data ? (
        <Spinner label="Loading packages…" />
      ) : packages.data && packages.data.length === 0 ? (
        <EmptyState title="No packages match your search">Try another destination or a longer duration.</EmptyState>
      ) : (
        <div className="card-grid">
          {packages.data?.map((p) => (
            <Link key={p.id} to={`/packages/${p.id}`} className="card listing">
              <ListingImage src={p.imageUrl} />
              <div className="listing-body">
                <div className="listing-title">
                  <h3>{p.title}</h3>
                  {p.approvalStatus !== ApprovalStatus.Approved && <ApprovalBadge status={p.approvalStatus} />}
                </div>
                <p className="muted">
                  {p.destinationName}, {p.destinationCountry} · {p.durationDays} day{p.durationDays === 1 ? '' : 's'}
                </p>
                <p className="muted small">{formatRating(p.averageRating, p.reviewCount)}</p>
                <p className="price">
                  <strong>{formatMoney(p.totalPrice)}</strong> / person
                </p>
              </div>
            </Link>
          ))}
        </div>
      )}
    </section>
  );
}
