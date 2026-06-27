import { useState } from 'react'
import './App.css'
import { useAuth } from './context/AuthContext'
import { LoginPage } from './components/LoginPage'
import { TransactionsList } from './components/TransactionsList'
import { DisputeForm } from './components/DisputeForm'
import { DisputeHistory } from './components/DisputeHistory'

const ROLE_BADGE_COLORS = {
  Admin:    '#dc2626',
  Banker:   '#2563eb',
  Client:   '#16a34a',
  ReadOnly: '#6b7280',
};

function App() {
  const { user, logout, isAuthenticated } = useAuth()
  const [selectedTransaction, setSelectedTransaction] = useState(null)
  const [activeTab, setActiveTab] = useState('transactions')
  const [refreshKey, setRefreshKey] = useState(0)

  if (!isAuthenticated) {
    return <LoginPage />
  }

  const handleDisputeCreated = () => {
    setSelectedTransaction(null)
    setActiveTab('history')
    setRefreshKey(prev => prev + 1)
  }

  const badgeColor = ROLE_BADGE_COLORS[user?.role] ?? '#6b7280'

  return (
    <div className="app">
      <header className="app-header">
        <div className="header-content">
          <h1>Capitec Transaction Dispute Portal</h1>
          <p className="subtitle">Manage your transactions and disputes</p>
        </div>
        <div className="header-user">
          <span className="user-name">{user?.fullName}</span>
          <span
            className="role-badge"
            style={{ backgroundColor: badgeColor }}
          >
            {user?.role}
          </span>
          <button className="logout-btn" onClick={logout}>
            Sign Out
          </button>
        </div>
      </header>

      <nav className="app-nav">
        <button
          className={`nav-btn ${activeTab === 'transactions' ? 'active' : ''}`}
          onClick={() => setActiveTab('transactions')}
        >
          💳 Transactions
        </button>
        <button
          className={`nav-btn ${activeTab === 'history' ? 'active' : ''}`}
          onClick={() => setActiveTab('history')}
        >
          📋 Dispute History
        </button>
      </nav>

      <main className="app-content">
        {activeTab === 'transactions' && (
          <div className="tab-content">
            {!selectedTransaction ? (
              <TransactionsList onSelectTransaction={setSelectedTransaction} />
            ) : (
              <DisputeForm
                transaction={selectedTransaction}
                onDisputeCreated={handleDisputeCreated}
                onCancel={() => setSelectedTransaction(null)}
              />
            )}
          </div>
        )}

        {activeTab === 'history' && (
          <div className="tab-content">
            <DisputeHistory key={refreshKey} />
          </div>
        )}
      </main>

      <footer className="app-footer">
        <p>&copy; 2026 Capitec Transaction Dispute Portal. All rights reserved.</p>
      </footer>
    </div>
  )
}

export default App
