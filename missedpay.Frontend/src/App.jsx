import { useState, useEffect } from 'react';
import AccountCard from './components/AccountCard';
import TransactionList from './components/TransactionList';
import BudgetingView from './components/BudgetingView';
import { accountsApi, transactionsApi, akahuApi } from './services/api';

function App() {
  const [accounts, setAccounts] = useState([]);
  const [transactions, setTransactions] = useState([]);
  const [selectedAccount, setSelectedAccount] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [refreshing, setRefreshing] = useState(false);
  const [refreshStatus, setRefreshStatus] = useState(null);
  const [activeView, setActiveView] = useState('accounts');

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      setError(null);
      const [accountsData, transactionsData] = await Promise.all([
        accountsApi.getAll(),
        transactionsApi.getAll(),
      ]);
      setAccounts(accountsData);
      setTransactions(transactionsData);
    } catch (err) {
      setError(err.message);
      console.error('Error loading data:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleAccountClick = (account) => {
    setSelectedAccount(selectedAccount?._id === account._id ? null : account);
  };

  const handleRefresh = async () => {
    try {
      setRefreshing(true);
      setRefreshStatus(null);
      setError(null);
      
      // Call Akahu refresh-all endpoint
      const result = await akahuApi.refreshAll();
      
      // Show success message with details
      setRefreshStatus({
        type: 'success',
        message: `Successfully refreshed! ${result.accountsResult?.accountsCreated || 0} new accounts, ${result.accountsResult?.accountsUpdated || 0} updated. ${result.transactionsResult?.transactionsCreated || 0} new transactions, ${result.transactionsResult?.transactionsUpdated || 0} updated.`
      });
      
      // Reload data to show updated accounts and transactions
      await loadData();
    } catch (err) {
      setRefreshStatus({
        type: 'error',
        message: `Failed to refresh: ${err.message}`
      });
      console.error('Error refreshing from Akahu:', err);
    } finally {
      setRefreshing(false);
      
      // Clear status message after 5 seconds
      setTimeout(() => {
        setRefreshStatus(null);
      }, 5000);
    }
  };

  const calculateTotalBalance = () => {
    return accounts.reduce((sum, account) => sum + (account.balance?.current || 0), 0);
  };

  const getTransactionsLast31Days = () => {
    const cutoffDate = new Date();
    cutoffDate.setDate(cutoffDate.getDate() - 31);
    return transactions.filter(t => new Date(t.date) >= cutoffDate);
  };

  const calculateTotalIncome = () => {
    return getTransactionsLast31Days()
      .filter(t => t.amount > 0)
      .reduce((sum, t) => sum + t.amount, 0);
  };

  const calculateTotalExpenses = () => {
    return getTransactionsLast31Days()
      .filter(t => t.amount < 0)
      .reduce((sum, t) => sum + Math.abs(t.amount), 0);
  };

  const getTransactionCount = (accountId) => {
    return transactions.filter(t => t._account === accountId).length;
  };

  const formatCurrency = (amount, currency = 'NZD') => {
    return '$' + new Intl.NumberFormat('en-US', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(amount);
  };

  if (loading) {
    return (
      <div style={{ 
        maxWidth: '1200px', 
        margin: '0 auto', 
        padding: '40px 20px',
        fontFamily: 'system-ui, -apple-system, sans-serif'
      }}>
        <p style={{ fontSize: '16px', color: '#666' }}>Loading your accounts...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div style={{ 
        maxWidth: '1200px', 
        margin: '0 auto', 
        padding: '40px 20px',
        fontFamily: 'system-ui, -apple-system, sans-serif'
      }}>
        <h2 style={{ fontSize: '24px', marginBottom: '12px', color: '#111' }}>Oops! Something went wrong</h2>
        <p style={{ fontSize: '16px', color: '#666', marginBottom: '20px' }}>{error}</p>
        <button 
          onClick={loadData}
          style={{
            padding: '10px 20px',
            fontSize: '14px',
            backgroundColor: '#111',
            color: '#fff',
            border: 'none',
            borderRadius: '6px',
            cursor: 'pointer'
          }}
        >
          Try Again
        </button>
      </div>
    );
  }

  return (
    <div 
      id="app-container"
      data-component="app"
      style={{ 
        minHeight: '100vh',
        backgroundColor: '#f9fafb',
        fontFamily: 'system-ui, -apple-system, sans-serif'
      }}
    >
      <style>
        {`
          @keyframes spin {
            from { transform: rotate(0deg); }
            to { transform: rotate(360deg); }
          }
          .refresh-icon-spinning {
            animation: spin 2s linear infinite;
            opacity: 0.5;
          }
        `}
      </style>
      
      {/* Top Navigation Bar */}
      <div 
        id="top-navigation-bar"
        data-component="navigation"
        style={{
          backgroundColor: '#fff',
          borderBottom: '1px solid #e5e7eb',
          position: 'sticky',
          top: 0,
          zIndex: 10
        }}
      >
        <div style={{ 
          maxWidth: '1400px', 
          margin: '0 auto',
          padding: '0 20px',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          height: '64px'
        }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '32px' }}>
            <h1 style={{ 
              fontSize: '20px', 
              fontWeight: '600', 
              color: '#111',
              margin: 0
            }}>
              MissedPay
            </h1>
            
            <nav 
              id="view-navigation"
              data-element="view-tabs"
              style={{ display: 'flex', gap: '8px' }}
            >
              <button
                id="accounts-tab"
                data-tab="accounts"
                data-active={activeView === 'accounts'}
                onClick={() => setActiveView('accounts')}
                style={{
                  padding: '8px 16px',
                  fontSize: '14px',
                  fontWeight: '500',
                  backgroundColor: activeView === 'accounts' ? '#f3f4f6' : 'transparent',
                  color: activeView === 'accounts' ? '#111' : '#6b7280',
                  border: 'none',
                  borderRadius: '6px',
                  cursor: 'pointer',
                  transition: 'background-color 0.2s, color 0.2s'
                }}
                onMouseOver={(e) => {
                  if (activeView !== 'accounts') {
                    e.currentTarget.style.backgroundColor = '#f9fafb';
                  }
                }}
                onMouseOut={(e) => {
                  if (activeView !== 'accounts') {
                    e.currentTarget.style.backgroundColor = 'transparent';
                  }
                }}
              >
                Accounts
              </button>
              
              <button
                id="budgeting-tab"
                data-tab="budgeting"
                data-active={activeView === 'budgeting'}
                onClick={() => setActiveView('budgeting')}
                style={{
                  padding: '8px 16px',
                  fontSize: '14px',
                  fontWeight: '500',
                  backgroundColor: activeView === 'budgeting' ? '#f3f4f6' : 'transparent',
                  color: activeView === 'budgeting' ? '#111' : '#6b7280',
                  border: 'none',
                  borderRadius: '6px',
                  cursor: 'pointer',
                  transition: 'background-color 0.2s, color 0.2s'
                }}
                onMouseOver={(e) => {
                  if (activeView !== 'budgeting') {
                    e.currentTarget.style.backgroundColor = '#f9fafb';
                  }
                }}
                onMouseOut={(e) => {
                  if (activeView !== 'budgeting') {
                    e.currentTarget.style.backgroundColor = 'transparent';
                  }
                }}
              >
                Budgeting
              </button>
            </nav>
          </div>
          
          {/* Refresh Button */}
          <button
            id="refresh-button"
            data-element="refresh-button"
            data-refreshing={refreshing}
            onClick={handleRefresh}
            disabled={refreshing}
            title={refreshing ? 'Refreshing...' : 'Refresh from Akahu'}
            style={{
              padding: '0',
              fontSize: '24px',
              backgroundColor: 'transparent',
              color: refreshing ? '#9ca3af' : '#6b7280',
              border: 'none',
              borderRadius: '8px',
              cursor: refreshing ? 'not-allowed' : 'pointer',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              transition: 'color 0.2s, background-color 0.2s',
              width: '40px',
              height: '40px'
            }}
            onMouseOver={(e) => {
              if (!refreshing) {
                e.currentTarget.style.backgroundColor = '#f3f4f6';
                e.currentTarget.style.color = '#111';
              }
            }}
            onMouseOut={(e) => {
              if (!refreshing) {
                e.currentTarget.style.backgroundColor = 'transparent';
                e.currentTarget.style.color = '#6b7280';
              }
            }}
          >
            <span style={{ 
              display: 'inline-block',
              animation: refreshing ? 'spin 2s linear infinite' : 'none',
              opacity: refreshing ? '0.5' : '1'
            }}>
              ↻
            </span>
          </button>
        </div>
        
        {/* Refresh Status Message */}
        {refreshStatus && (
          <div 
            id="refresh-status-message"
            data-element="status-message"
            data-status-type={refreshStatus.type}
            style={{
              maxWidth: '1400px',
              margin: '0 auto',
              padding: '0 20px 12px',
            }}
          >
            <div style={{
              padding: '12px 16px',
              borderRadius: '6px',
              backgroundColor: refreshStatus.type === 'success' ? '#dcfce7' : '#fee2e2',
              border: `1px solid ${refreshStatus.type === 'success' ? '#86efac' : '#fca5a5'}`,
              fontSize: '14px',
              color: refreshStatus.type === 'success' ? '#166534' : '#991b1b'
            }}>
              {refreshStatus.message}
            </div>
          </div>
        )}
      </div>

      {/* Page Header */}
      <div 
        id="page-header"
        data-component="page-header"
        data-active-view={activeView}
        style={{
          backgroundColor: '#fff',
          borderBottom: '1px solid #e5e7eb',
          padding: '20px'
        }}
      >
        <div style={{ 
          maxWidth: '1400px', 
          margin: '0 auto'
        }}>
          <h2 style={{ 
            fontSize: '24px', 
            fontWeight: '600', 
            marginBottom: '4px',
            color: '#111'
          }}>
            {activeView === 'accounts' ? 'Bank Accounts' : 'Budgeting'}
          </h2>
          <p style={{ 
            fontSize: '14px', 
            color: '#6b7280',
            margin: 0
          }}>
            {activeView === 'accounts' 
              ? 'Manage your accounts and view transaction history'
              : 'Track your spending and manage budgets'}
          </p>
        </div>
      </div>

      {/* Main Content Area */}
      {activeView === 'accounts' ? (
        <div 
          id="accounts-view"
          data-component="accounts-view"
          style={{
            maxWidth: '1400px',
            margin: '0 auto',
            padding: '20px',
            display: 'flex',
            gap: '24px',
            flexWrap: 'wrap'
          }}
        >
          {/* Left Panel - Accounts */}
        <div 
          id="accounts-panel"
          data-panel="accounts"
          style={{
            flex: '1 1 400px',
            minWidth: '300px',
            maxWidth: selectedAccount ? '450px' : '100%'
          }}
        >
          {/* Total Balance Card */}
          <div 
            id="total-balance-card"
            data-component="balance-summary"
            style={{
              backgroundColor: '#fff',
              borderRadius: '12px',
              padding: '24px',
              marginBottom: '20px',
              boxShadow: '0 1px 3px rgba(0, 0, 0, 0.1)'
            }}
          >
            <div style={{ marginBottom: '20px' }}>
              <p style={{ 
                fontSize: '14px', 
                color: '#6b7280',
                margin: '0 0 8px 0',
                fontWeight: '500'
              }}>
                Total Balance
              </p>
              <p style={{ 
                fontSize: '32px', 
                fontWeight: '600',
                margin: 0,
                color: '#111'
              }}>
                {formatCurrency(calculateTotalBalance())}
              </p>
            </div>

            <div style={{
              display: 'flex',
              gap: '16px',
              paddingTop: '20px',
              borderTop: '1px solid #e5e7eb'
            }}>
              <div style={{ flex: 1 }}>
                <p style={{ 
                  fontSize: '12px', 
                  color: '#6b7280',
                  margin: '0 0 4px 0',
                  fontWeight: '500'
                }}>
                  Income (31 days)
                </p>
                <p style={{ 
                  fontSize: '18px', 
                  fontWeight: '600',
                  margin: 0,
                  color: '#059669'
                }}>
                  {formatCurrency(calculateTotalIncome())}
                </p>
              </div>

              <div style={{ flex: 1 }}>
                <p style={{ 
                  fontSize: '12px', 
                  color: '#6b7280',
                  margin: '0 0 4px 0',
                  fontWeight: '500'
                }}>
                  Expenses (31 days)
                </p>
                <p style={{ 
                  fontSize: '18px', 
                  fontWeight: '600',
                  margin: 0,
                  color: '#dc2626'
                }}>
                  {formatCurrency(calculateTotalExpenses())}
                </p>
              </div>
            </div>
          </div>

          {/* Accounts List */}
          <div 
            id="accounts-list-card"
            data-component="accounts-list"
            style={{
              backgroundColor: '#fff',
              borderRadius: '12px',
              padding: '20px',
              boxShadow: '0 1px 3px rgba(0, 0, 0, 0.1)'
            }}
          >
            <h2 style={{
              fontSize: '18px',
              fontWeight: '600',
              color: '#111',
              marginBottom: '16px'
            }}>
              Your Accounts
            </h2>
            
            {accounts.length === 0 ? (
              <div style={{
                textAlign: 'center',
                padding: '40px 20px',
                backgroundColor: '#f9fafb',
                borderRadius: '8px'
              }}>
                <h3 style={{ fontSize: '16px', marginBottom: '8px', color: '#111' }}>No accounts found</h3>
                <p style={{ fontSize: '14px', color: '#6b7280', margin: 0 }}>Add your first account to get started</p>
              </div>
            ) : (
              <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
                {accounts.map((account) => (
                  <AccountCard
                    key={account._id}
                    account={account}
                    isSelected={selectedAccount?._id === account._id}
                    onClick={handleAccountClick}
                    transactions={transactions.filter(t => t._account === account._id)}
                  />
                ))}
              </div>
            )}

            {!selectedAccount && accounts.length > 0 && (
              <div style={{
                marginTop: '16px',
                padding: '12px',
                backgroundColor: '#f9fafb',
                borderRadius: '8px',
                textAlign: 'center'
              }}>
                <p style={{ 
                  fontSize: '13px', 
                  color: '#6b7280',
                  margin: 0
                }}>
                  Click on any account to view transactions
                </p>
              </div>
            )}
          </div>
        </div>

        {/* Right Panel - Transactions */}
        {selectedAccount && (
          <div 
            id="transactions-panel"
            data-panel="transactions"
            data-account-id={selectedAccount._id}
            style={{
              flex: '1 1 600px',
              minWidth: '300px'
            }}
          >
            <div 
              id="transactions-card"
              data-component="transactions-container"
              style={{
                backgroundColor: '#fff',
                borderRadius: '12px',
                padding: '20px',
                boxShadow: '0 1px 3px rgba(0, 0, 0, 0.1)'
              }}
            >
              <TransactionList
                transactions={transactions}
                accountId={selectedAccount._id}
                account={selectedAccount}
              />
            </div>
          </div>
        )}

        {/* Placeholder when no account selected - only on desktop */}
        {!selectedAccount && accounts.length > 0 && (
          <div style={{
            flex: '1 1 600px',
            minWidth: '300px',
            display: 'none',
            '@media (min-width: 768px)': {
              display: 'block'
            }
          }}>
            <div style={{
              backgroundColor: '#fff',
              borderRadius: '12px',
              padding: '60px 40px',
              boxShadow: '0 1px 3px rgba(0, 0, 0, 0.1)',
              textAlign: 'center',
              height: '100%',
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              justifyContent: 'center'
            }}>
              <div style={{
                fontSize: '64px',
                marginBottom: '16px',
                opacity: 0.3
              }}>
                💳
              </div>
              <h3 style={{ 
                fontSize: '20px', 
                fontWeight: '600',
                marginBottom: '8px', 
                color: '#111' 
              }}>
                Select an Account
              </h3>
              <p style={{ 
                fontSize: '14px', 
                color: '#6b7280',
                margin: 0
              }}>
                Choose an account from the left to view its transaction history
              </p>
            </div>
          </div>
        )}
        </div>
      ) : (
        /* Budgeting View */
        <BudgetingView transactions={transactions} onTransactionUpdate={loadData} />
      )}
    </div>
  );
}

export default App;
