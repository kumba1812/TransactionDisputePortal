import { useState, useEffect, useCallback } from 'react';
import { disputeApi } from '../services/api';
import { getStatusLabel, formatDate } from '../utils/statusHelpers';
import '../styles/DisputeStatusModal.css';

const LOCK_EXPIRY_MINUTES = 10;

function useLockCountdown(lockedAt) {
  const [secondsLeft, setSecondsLeft] = useState(() => {
    if (!lockedAt) return 0;
    const expiresAt = new Date(lockedAt).getTime() + LOCK_EXPIRY_MINUTES * 60 * 1000;
    return Math.max(0, Math.floor((expiresAt - Date.now()) / 1000));
  });

  useEffect(() => {
    if (secondsLeft <= 0) return;
    const id = setInterval(() => {
      setSecondsLeft(prev => Math.max(0, prev - 1));
    }, 1000);
    return () => clearInterval(id);
  }, [secondsLeft]);

  const minutes = String(Math.floor(secondsLeft / 60)).padStart(2, '0');
  const seconds = String(secondsLeft % 60).padStart(2, '0');
  return { display: `${minutes}:${seconds}`, expired: secondsLeft <= 0 };
}

export function DisputeStatusModal({ dispute, onClose, onStatusUpdated }) {
  const [loading, setLoading] = useState(false);
  const [error, setError]     = useState(null);
  const [selectedStatus, setSelectedStatus] = useState(dispute.status);
  const [resolutionNotes, setResolutionNotes] = useState(dispute.resolutionNotes || '');

  const lock = useLockCountdown(dispute.lockedAt);

  const statusOptions = [
    { value: 0, label: 'Pending' },
    { value: 1, label: 'Under Review' },
    { value: 2, label: 'Resolved' },
    { value: 3, label: 'Refunded' },
    { value: 4, label: 'Rejected' },
  ];

  const handleClose = useCallback(async () => {
    try { await disputeApi.releaseLock(dispute.id); } catch { /* ignore */ }
    onClose();
  }, [dispute.id, onClose]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError(null);

    if (lock.expired) {
      setError('Your lock has expired. Please close and reopen the dispute to edit it.');
      return;
    }

    if (selectedStatus === dispute.status && resolutionNotes === (dispute.resolutionNotes || '')) {
      setError('No changes made');
      return;
    }

    try {
      setLoading(true);
      await disputeApi.updateDispute(dispute.id, {
        status: selectedStatus,
        resolutionNotes: resolutionNotes || null,
      });
      onStatusUpdated();
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to update dispute status');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="modal-overlay">
      <div className="modal-content">
        <div className="modal-header">
          <h2>Update Dispute Status</h2>
          <button className="close-btn" onClick={handleClose} disabled={loading}>×</button>
        </div>

        <div className="dispute-info">
          <p><strong>Dispute ID:</strong> {dispute.id}</p>
          <p><strong>Reason:</strong> {dispute.reason}</p>
          <p><strong>Created:</strong> {formatDate(dispute.createdAt)}</p>
          <p><strong>Current Status:</strong> {getStatusLabel(dispute.status)}</p>
        </div>

        <div className={`lock-countdown ${lock.expired ? 'expired' : lock.display < '01:00' ? 'warning' : ''}`}>
          🔒 {lock.expired ? 'Lock expired — save blocked' : `Lock expires in ${lock.display}`}
        </div>

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="status">New Status *</label>
            <select
              id="status"
              value={selectedStatus}
              onChange={(e) => setSelectedStatus(Number(e.target.value))}
              disabled={loading || lock.expired}
              required
            >
              {statusOptions.map(option => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label htmlFor="notes">Resolution Notes</label>
            <textarea
              id="notes"
              value={resolutionNotes}
              onChange={(e) => setResolutionNotes(e.target.value)}
              placeholder="Add notes about the resolution..."
              disabled={loading || lock.expired}
              rows="4"
            />
          </div>

          {error && <div className="error-message">{error}</div>}

          <div className="form-actions">
            <button
              type="button"
              className="cancel-btn"
              onClick={handleClose}
              disabled={loading}
            >
              Cancel
            </button>
            <button
              type="submit"
              className="submit-btn"
              disabled={loading || lock.expired}
            >
              {loading ? 'Updating...' : 'Update Status'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
