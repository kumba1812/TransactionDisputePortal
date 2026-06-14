import { useState, useEffect } from 'react';
import { disputeApi } from '../services/api';
import '../styles/DisputeForm.css';

export function DisputeForm({ transaction, onDisputeCreated, onCancel }) {
  const [formData, setFormData] = useState({
    reason: '',
    description: '',
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [success, setSuccess] = useState(false);
  const [existingDisputes, setExistingDisputes] = useState([]);

  const reasons = [
    'Unauthorized',
    'Duplicate Charge',
    'Incorrect Amount',
    'Service Not Provided',
    'Merchandise Not Received',
    'Other'
  ];

  useEffect(() => {
    fetchExistingDisputes();
  }, [transaction]);

  const fetchExistingDisputes = async () => {
    try {
      const response = await disputeApi.getDisputesByTransaction(transaction.id);
      setExistingDisputes(response.data);
    } catch (err) {
      console.error('Failed to fetch disputes:', err);
    }
  };

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value
    }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError(null);
    setSuccess(false);

    if (!formData.reason || !formData.description) {
      setError('Please fill in all fields');
      return;
    }

    try {
      setLoading(true);
      await disputeApi.createDispute({
        transactionId: transaction.id,
        reason: formData.reason,
        description: formData.description,
      });
      setSuccess(true);
      setFormData({ reason: '', description: '' });
      setTimeout(() => {
        onDisputeCreated();
      }, 1500);
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to create dispute');
    } finally {
      setLoading(false);
    }
  };

  const hasPendingDispute = existingDisputes.length > 0;

  return (
    <div className="dispute-form-container">
      <div className="form-header">
        <h2>Dispute Transaction</h2>
        <button className="close-btn" onClick={onCancel}>×</button>
      </div>

      <div className="transaction-details">
        <p><strong>Merchant:</strong> {transaction.merchant}</p>
        <p><strong>Amount:</strong> ${transaction.amount.toFixed(2)}</p>
        <p><strong>Date:</strong> {new Date(transaction.transactionDate).toLocaleDateString()}</p>
        <p><strong>Description:</strong> {transaction.description}</p>
      </div>

      {hasPendingDispute && (
        <div className="warning">
          <strong>Note:</strong> This transaction has an existing dispute.
        </div>
      )}

      <form onSubmit={handleSubmit}>
        <div className="form-group">
          <label htmlFor="reason">Reason for Dispute *</label>
          <select
            id="reason"
            name="reason"
            value={formData.reason}
            onChange={handleChange}
            disabled={loading}
            required
          >
            <option value="">Select a reason...</option>
            {reasons.map(reason => (
              <option key={reason} value={reason}>{reason}</option>
            ))}
          </select>
        </div>

        <div className="form-group">
          <label htmlFor="description">Description *</label>
          <textarea
            id="description"
            name="description"
            value={formData.description}
            onChange={handleChange}
            placeholder="Please provide detailed information about your dispute..."
            disabled={loading}
            required
            rows="6"
          />
        </div>

        {error && <div className="error-message">{error}</div>}
        {success && <div className="success-message">Dispute created successfully!</div>}

        <div className="form-actions">
          <button 
            type="button" 
            className="cancel-btn"
            onClick={onCancel}
            disabled={loading}
          >
            Cancel
          </button>
          <button 
            type="submit" 
            className="submit-btn"
            disabled={loading || hasPendingDispute}
          >
            {loading ? 'Creating...' : 'Create Dispute'}
          </button>
        </div>
      </form>
    </div>
  );
}
