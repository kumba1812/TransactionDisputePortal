import { useState, useEffect } from 'react';
import { disputeApi } from '../services/api';
import { getStatusLabel, formatCurrency, formatDate } from '../utils/statusHelpers';
import { DisputeStatusModal } from './DisputeStatusModal';
import '../styles/DisputeHistory.css';

export function DisputeHistory() {
  const [disputes, setDisputes] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [filter, setFilter] = useState('all');
  const [selectedDispute, setSelectedDispute] = useState(null);

  useEffect(() => {
    fetchDisputes();
  }, []);

  const fetchDisputes = async () => {
    try {
      setLoading(true);
      const response = await disputeApi.getDisputes();
      setDisputes(response.data);
      setError(null);
    } catch (err) {
      setError('Failed to load dispute history ' + (err.response?.data?.message || err.message));
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const getStatusColor = (status) => {
    const statusLabel = getStatusLabel(status).toLowerCase();
    switch (statusLabel) {
      case 'pending':
        return '#ff9800';
      case 'underreview':
        return '#2196f3';
      case 'resolved':
      case 'refunded':
        return '#4caf50';
      case 'rejected':
        return '#f44336';
      default:
        return '#999';
    }
  };

  const filteredDisputes = disputes.filter(dispute => {
    if (filter === 'all') return true;
    return getStatusLabel(dispute.status).toLowerCase() === filter.toLowerCase();
  });

  const statusOptions = ['all', 'pending', 'underreview', 'resolved', 'rejected', 'refunded'];

  const handleStatusUpdated = () => {
    setSelectedDispute(null);
    fetchDisputes();
  };

  if (loading) return <div className="loading">Loading dispute history...</div>;
  if (error) return <div className="error">{error}</div>;

  return (
    <div className="dispute-history">
      <div className="history-header">
        <h2>Dispute History</h2>
        <button className="refresh-btn" onClick={fetchDisputes}>↻ Refresh</button>
      </div>

      <div className="filter-group">
        <label>Filter by Status:</label>
        <div className="filter-buttons">
          {statusOptions.map(status => (
            <button
              key={status}
              className={`filter-btn ${filter === status ? 'active' : ''}`}
              onClick={() => setFilter(status)}
            >
              {status.charAt(0).toUpperCase() + status.slice(1)}
            </button>
          ))}
        </div>
      </div>

      {filteredDisputes.length === 0 ? (
        <p className="no-data">No disputes found</p>
      ) : (
        <div className="disputes-grid">
          {filteredDisputes.map((dispute) => (
            <div key={dispute.id} className="dispute-card">
              <div className="card-header">
                <h3>{dispute.reason}</h3>
                <span 
                  className="status-badge"
                  style={{ backgroundColor: getStatusColor(dispute.status) }}
                >
                  {getStatusLabel(dispute.status)}
                </span>
              </div>

              <div className="card-body">
                <p className="description">{dispute.description}</p>

                <div className="dispute-details">
                  <div className="detail-row">
                    <span className="label">Merchant:</span>
                    <span className="value">{dispute.transaction?.merchant || 'N/A'}</span>
                  </div>
                  <div className="detail-row">
                    <span className="label">Amount:</span>
                    <span className="value">{formatCurrency(dispute.refundAmount)}</span>
                  </div>
                  <div className="detail-row">
                    <span className="label">Created:</span>
                    <span className="value">{formatDate(dispute.createdAt)}</span>
                  </div>
                  {dispute.resolvedAt && (
                    <div className="detail-row">
                      <span className="label">Resolved:</span>
                      <span className="value">{formatDate(dispute.resolvedAt)}</span>
                    </div>
                  )}
                </div>

                {dispute.resolutionNotes && (
                  <div className="resolution-notes">
                    <strong>Resolution Notes:</strong>
                    <p>{dispute.resolutionNotes}</p>
                  </div>
                )}

                <button 
                  className="update-status-btn"
                  onClick={() => setSelectedDispute(dispute)}
                >
                  Update Status
                </button>
              </div>
            </div>
          ))}
        </div>
      )}

      {selectedDispute && (
        <DisputeStatusModal 
          dispute={selectedDispute}
          onClose={() => setSelectedDispute(null)}
          onStatusUpdated={handleStatusUpdated}
        />
      )}
    </div>
  );
}
