import { useState, useEffect } from 'react';
import { transactionApi, disputeApi } from '../services/api';
import { getTransactionStatusLabel, getStatusLabel, formatCurrency, formatDate } from '../utils/statusHelpers';
import { useAuth } from '../context/AuthContext';
import '../styles/TransactionsList.css';

const DISPUTE_LABEL = { pending: 'Pending', underreview: 'Under Review', resolved: 'Resolved', refunded: 'Refunded', rejected: 'Rejected' };

export function TransactionsList({ onSelectTransaction }) {
  const [transactions, setTransactions] = useState([]);
  const [disputeMap, setDisputeMap] = useState({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const { user } = useAuth();

  // Bankers and ReadOnly users cannot file disputes
  const canDispute = user?.role === 'Client' || user?.role === 'Admin';

  useEffect(() => {
    fetchTransactions();
  }, []);

  const fetchTransactions = async () => {
    try {
      setLoading(true);
      const [txnRes, dispRes] = await Promise.all([
        transactionApi.getTransactions(),
        disputeApi.getDisputes(),
      ]);
      setTransactions(txnRes.data);

      // Build a map of transactionIdFk → most-recent dispute for quick lookup
      const map = {};
      for (const d of dispRes.data) {
        const key = d.transactionIdFk;
        // Keep the most recent dispute per transaction (favour non-resolved)
        if (!map[key] || d.status < map[key].status) {
          map[key] = d;
        }
      }
      setDisputeMap(map);
      setError(null);
    } catch (err) {
      setError('Failed to load transactions');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  if (loading) return <div className="loading">Loading transactions...</div>;
  if (error) return <div className="error">{error}</div>;

  return (
    <div className="transactions-list">
      <div className="list-header">
        <h2>Recent Transactions</h2>
        <button className="refresh-btn" onClick={fetchTransactions}>↻ Refresh</button>
      </div>
      {transactions.length === 0 ? (
        <p className="no-data">No transactions found</p>
      ) : (
        <div className="table-container">
          <table className="transactions-table">
            <thead>
              <tr>
                <th>Date</th>
                <th>Merchant</th>
                <th>Amount</th>
                <th>Status</th>
                <th>Dispute</th>
                {canDispute && <th>Action</th>}
              </tr>
            </thead>
            <tbody>
              {transactions.map((transaction) => {
                const dispute = disputeMap[transaction.id];
                const disputeKey = dispute ? getStatusLabel(dispute.status) : null;
                return (
                  <tr key={transaction.id}>
                    <td>{formatDate(transaction.transactionDate)}</td>
                    <td>{transaction.merchant}</td>
                    <td className="amount">{formatCurrency(transaction.amount)}</td>
                    <td>
                      <span className={`status status-${getTransactionStatusLabel(transaction.status)?.toLowerCase()}`}>
                        {getTransactionStatusLabel(transaction.status)}
                      </span>
                    </td>
                    <td>
                      {dispute ? (
                        <span className={`dispute-badge dispute-badge-${disputeKey}`}>
                          {DISPUTE_LABEL[disputeKey] ?? disputeKey}
                        </span>
                      ) : (
                        <span className="no-dispute">—</span>
                      )}
                    </td>
                    {canDispute && (
                      <td>
                        <button
                          className="dispute-btn"
                          onClick={() => onSelectTransaction(transaction)}
                        >
                          View/Dispute
                        </button>
                      </td>
                    )}
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
