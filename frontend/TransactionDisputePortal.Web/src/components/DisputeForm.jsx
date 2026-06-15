import { useState, useEffect } from 'react';
import { disputeApi } from '../services/api';
import { formatCurrency, formatDate } from '../utils/statusHelpers';
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

  const bankingReasons = [
    'Unauthorized Transaction',
    'Duplicate Charge',
    'Incorrect Amount',
    'ATM Malfunction',
    'Fraudulent Activity',
    'Missing Deposit',
    'Wire Transfer Error',
    'Reversal Request',
    'Billing Error',
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

    if (formData.description.trim().length < 10) {
      setError('Description must be at least 10 characters');
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
        <div className="detail-item">
          <span className="label">Merchant:</span>
          <span className="value">{transaction.merchant}</span>
        </div>
        <div className="detail-item">
          <span className="label">Amount:</span>
          <span className="value amount">{formatCurrency(transaction.amount)}</span>
        </div>
        <div className="detail-item">
          <span className="label">Date:</span>
          <span className="value">{formatDate(transaction.transactionDate)}</span>
        </div>
        <div className="detail-item">
          <span className="label">Category:</span>
          <span className="value">{transaction.category}</span>
        </div>
        <div className="detail-item">
          <span className="label">Description:</span>
          <span className="value">{transaction.description}</span>
        </div>
      </div>

      {hasPendingDispute && (
        <div className="warning">
          <strong>⚠️ Alert:</strong> This transaction already has an active dispute. You cannot create a duplicate dispute.
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
            disabled={loading || hasPendingDispute}
            required
          >
            <option value="">Select a reason...</option>
            {bankingReasons.map(reason => (
              <option key={reason} value={reason}>{reason}</option>
            ))}
          </select>
        </div>

        <div className="form-group">
          <label htmlFor="description">Description of Dispute *</label>
          <textarea
            id="description"
            name="description"
            value={formData.description}
            onChange={handleChange}
            placeholder="Please provide detailed information about your dispute. Include any relevant dates, amounts, or reference numbers..."
            disabled={loading || hasPendingDispute}
            required
            rows="6"
            minLength="10"
          />
          <small className="char-count">{formData.description.length} characters (minimum 10)</small>
        </div>

        {error && <div className="error-message">{error}</div>}
        {success && <div className="success-message">✓ Dispute created successfully! Redirecting...</div>}

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
            {loading ? 'Creating Dispute...' : 'Create Dispute'}
          </button>
        </div>
      </form>
    </div>
  );
}
