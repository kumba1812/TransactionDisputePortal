import { useState } from 'react';
import { disputeApi } from '../services/api';
import { getStatusLabel, formatDate } from '../utils/statusHelpers';
import '../styles/DisputeStatusModal.css';

export function DisputeStatusModal({ dispute, onClose, onStatusUpdated }) {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [selectedStatus, setSelectedStatus] = useState(dispute.status);
  const [resolutionNotes, setResolutionNotes] = useState(dispute.resolutionNotes || '');

  const statusOptions = [
    { value: 0, label: 'Pending' },
    { value: 1, label: 'Under Review' },
    { value: 2, label: 'Resolved' },
    { value: 3, label: 'Refunded' },
    { value: 4, label: 'Rejected' },
  ];

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError(null);

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
          <button className="close-btn" onClick={onClose}>×</button>
        </div>

        <div className="dispute-info">
          <p><strong>Dispute ID:</strong> {dispute.id}</p>
          <p><strong>Reason:</strong> {dispute.reason}</p>
          <p><strong>Created:</strong> {formatDate(dispute.createdAt)}</p>
          <p><strong>Current Status:</strong> {getStatusLabel(dispute.status)}</p>
        </div>

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="status">New Status *</label>
            <select
              id="status"
              value={selectedStatus}
              onChange={(e) => setSelectedStatus(Number(e.target.value))}
              disabled={loading}
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
              disabled={loading}
              rows="4"
            />
          </div>

          {error && <div className="error-message">{error}</div>}

          <div className="form-actions">
            <button 
              type="button" 
              className="cancel-btn"
              onClick={onClose}
              disabled={loading}
            >
              Cancel
            </button>
            <button 
              type="submit" 
              className="submit-btn"
              disabled={loading}
            >
              {loading ? 'Updating...' : 'Update Status'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
