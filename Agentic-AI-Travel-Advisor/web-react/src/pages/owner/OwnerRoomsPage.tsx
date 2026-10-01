import { useState, type FormEvent } from 'react';
import { Link, useParams } from 'react-router-dom';
import { hotelsApi, roomsApi } from '../../api/services';
import type { Room, SaveRoomRequest } from '../../api/types';
import { ActionFeedback, Alert, ApprovalBadge, Badge, EmptyState, Field, PageHeader, Spinner } from '../../components/ui';
import { formatMoney } from '../../lib/format';
import { useAction } from '../../lib/useAction';
import { useAsync } from '../../lib/useAsync';

export function OwnerRoomsPage() {
  const hotelId = Number(useParams().id);
  const hotel = useAsync(() => hotelsApi.get(hotelId), [hotelId]);
  const [editing, setEditing] = useState<Room | 'new' | null>(null);
  const action = useAction();

  const rooms = hotel.data?.rooms ?? [];
  const replaceRoom = (room: Room) =>
    hotel.setData((h) => {
      if (!h) throw new Error('Hotel not loaded');
      const exists = h.rooms.some((r) => r.id === room.id);
      return { ...h, rooms: exists ? h.rooms.map((r) => (r.id === room.id ? room : r)) : [...h.rooms, room] };
    });

  async function toggle(room: Room) {
    await action.run(
      async () => replaceRoom(await roomsApi.setAvailability(hotelId, room.id, !room.isAvailable)),
      `${room.name} is now ${room.isAvailable ? 'closed' : 'open'} for booking.`,
    );
  }

  async function remove(room: Room) {
    if (!window.confirm(`Delete ${room.name}?`)) return;
    const ok = await action.run(() => roomsApi.remove(hotelId, room.id), `${room.name} deleted.`);
    if (ok) hotel.setData((h) => (h ? { ...h, rooms: h.rooms.filter((r) => r.id !== room.id) } : h!));
  }

  if (hotel.loading && !hotel.data) return <Spinner />;
  if (hotel.error || !hotel.data) return <Alert>{hotel.error ?? 'Hotel not found.'}</Alert>;

  return (
    <>
      <Link to="/owner/hotels" className="back-link">
        ← My hotels
      </Link>
      <PageHeader
        title={hotel.data.name}
        subtitle={
          <>
            {hotel.data.city}, {hotel.data.country} · <ApprovalBadge status={hotel.data.approvalStatus} />
          </>
        }
        actions={
          <button className="btn btn-primary" onClick={() => setEditing('new')}>
            + Add room
          </button>
        }
      />
      <ActionFeedback error={action.error} success={action.success} onClose={action.clear} />
      {editing && (
        <RoomForm
          hotelId={hotelId}
          room={editing === 'new' ? null : editing}
          onCancel={() => setEditing(null)}
          onSaved={(room) => {
            replaceRoom(room);
            setEditing(null);
          }}
        />
      )}
      {rooms.length === 0 ? (
        <EmptyState title="No rooms yet">Add rooms so guests can book this hotel.</EmptyState>
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Room</th>
                <th>Type</th>
                <th>Sleeps</th>
                <th>Price / night</th>
                <th>Booking</th>
                <th aria-label="Actions" />
              </tr>
            </thead>
            <tbody>
              {rooms.map((r) => (
                <tr key={r.id}>
                  <td>{r.name}</td>
                  <td>{r.roomType}</td>
                  <td>{r.capacity}</td>
                  <td>{formatMoney(r.pricePerNight)}</td>
                  <td>{r.isAvailable ? <Badge tone="success">Open</Badge> : <Badge>Closed</Badge>}</td>
                  <td className="row-actions">
                    <button className="btn btn-secondary btn-sm" onClick={() => toggle(r)} disabled={action.busy}>
                      {r.isAvailable ? 'Close' : 'Open'}
                    </button>
                    <button className="btn btn-ghost btn-sm" onClick={() => setEditing(r)}>
                      Edit
                    </button>
                    <button className="btn btn-danger btn-sm" onClick={() => remove(r)} disabled={action.busy}>
                      Delete
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  );
}

function RoomForm({ hotelId, room, onSaved, onCancel }: { hotelId: number; room: Room | null; onSaved: (r: Room) => void; onCancel: () => void }) {
  const [form, setForm] = useState({
    name: room?.name ?? '',
    roomType: room?.roomType ?? 'Double',
    pricePerNight: room?.pricePerNight.toString() ?? '',
    capacity: room?.capacity.toString() ?? '2',
  });
  const [formError, setFormError] = useState<string | null>(null);
  const action = useAction();
  const set = (key: keyof typeof form) => (e: React.ChangeEvent<HTMLInputElement>) => setForm((f) => ({ ...f, [key]: e.target.value }));

  async function submit(e: FormEvent) {
    e.preventDefault();
    const body: SaveRoomRequest = {
      name: form.name.trim(),
      roomType: form.roomType.trim(),
      pricePerNight: Number(form.pricePerNight),
      capacity: Number(form.capacity),
    };
    if (!body.name || !body.roomType) return setFormError('Name and type are required.');
    if (!(body.pricePerNight > 0)) return setFormError('Price must be greater than zero.');
    if (!Number.isInteger(body.capacity) || body.capacity < 1) return setFormError('Capacity must be a whole number of at least 1.');
    setFormError(null);
    await action.run(async () => onSaved(room ? await roomsApi.update(hotelId, room.id, body) : await roomsApi.create(hotelId, body)));
  }

  return (
    <form className="card form-card" onSubmit={submit} noValidate>
      <h2>{room ? `Edit ${room.name}` : 'Add a room'}</h2>
      <div className="grid-4">
        <Field label="Name">
          <input value={form.name} onChange={set('name')} />
        </Field>
        <Field label="Type">
          <input value={form.roomType} onChange={set('roomType')} />
        </Field>
        <Field label="Price / night (LKR)">
          <input type="number" min={1} value={form.pricePerNight} onChange={set('pricePerNight')} />
        </Field>
        <Field label="Sleeps">
          <input type="number" min={1} value={form.capacity} onChange={set('capacity')} />
        </Field>
      </div>
      <ActionFeedback error={formError ?? action.error} success={null} />
      <div className="row-actions">
        <button className="btn btn-primary" type="submit" disabled={action.busy}>
          {room ? 'Save room' : 'Add room'}
        </button>
        <button className="btn btn-ghost" type="button" onClick={onCancel}>
          Cancel
        </button>
      </div>
    </form>
  );
}
