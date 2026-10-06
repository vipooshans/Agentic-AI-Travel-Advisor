import { useState } from 'react';
import { settingsApi } from '../../api/services';
import type { SystemSetting } from '../../api/types';
import { ActionFeedback, Alert, PageHeader, Spinner } from '../../components/ui';
import { formatDateTime } from '../../lib/format';
import { useAction } from '../../lib/useAction';
import { useAsync } from '../../lib/useAsync';

export function AdminSettingsPage() {
  const settings = useAsync(() => settingsApi.list(), []);
  return (
    <>
      <PageHeader title="System settings" subtitle="Changes apply immediately to the API, the web app and the mobile app." />
      {settings.error && <Alert>{settings.error}</Alert>}
      {!settings.data ? (
        !settings.error && <Spinner />
      ) : (
        <div className="stack">
          {settings.data.map((s) => (
            <SettingRow
              key={s.key}
              setting={s}
              onSaved={(updated) => settings.setData((list) => (list ?? []).map((x) => (x.key === updated.key ? updated : x)))}
            />
          ))}
        </div>
      )}
    </>
  );
}

function SettingRow({ setting, onSaved }: { setting: SystemSetting; onSaved: (s: SystemSetting) => void }) {
  const [value, setValue] = useState(setting.value);
  const action = useAction();
  const isBoolean = setting.value === 'true' || setting.value === 'false';

  async function save(next: string) {
    await action.run(async () => {
      const updated = await settingsApi.update(setting.key, next);
      setValue(updated.value);
      onSaved(updated);
    }, 'Saved.');
  }

  return (
    <div className="card setting">
      <div className="setting-info">
        <code>{setting.key}</code>
        {setting.description && <p className="muted small">{setting.description}</p>}
        <p className="muted small">
          Updated {formatDateTime(setting.updatedAt)}
          {setting.updatedBy && ` by ${setting.updatedBy}`}
        </p>
      </div>
      <div className="setting-control">
        {isBoolean ? (
          <label className="checkbox">
            <input
              type="checkbox"
              checked={value === 'true'}
              disabled={action.busy}
              onChange={(e) => save(e.target.checked ? 'true' : 'false')}
              aria-label={setting.key}
            />
            {value === 'true' ? 'Enabled' : 'Disabled'}
          </label>
        ) : (
          <form
            className="inline-form"
            onSubmit={(e) => {
              e.preventDefault();
              void save(value);
            }}
          >
            <input aria-label={setting.key} value={value} onChange={(e) => setValue(e.target.value)} />
            <button className="btn btn-primary btn-sm" type="submit" disabled={action.busy || value === setting.value}>
              Save
            </button>
          </form>
        )}
        <ActionFeedback error={action.error} success={action.success} onClose={action.clear} />
      </div>
    </div>
  );
}
