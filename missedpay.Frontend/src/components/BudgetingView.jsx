import { useState, useMemo } from 'react';

const BudgetingView = ({ transactions }) => {
  const [selectedGroup, setSelectedGroup] = useState(null);
  const [selectedCategory, setSelectedCategory] = useState(null);
  const [sortOrder, setSortOrder] = useState('date'); // 'date' or 'amount'

  const formatCurrency = (amount) => {
    return '$' + new Intl.NumberFormat('en-US', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(Math.abs(amount));
  };

  const formatDate = (dateString) => {
    const date = new Date(dateString);
    const today = new Date();
    const yesterday = new Date(today);
    yesterday.setDate(yesterday.getDate() - 1);

    if (date.toDateString() === today.toDateString()) {
      return 'Today';
    } else if (date.toDateString() === yesterday.toDateString()) {
      return 'Yesterday';
    } else {
      return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
    }
  };

  // Aggregate transactions by personal_finance group
  const groupAggregates = useMemo(() => {
    const groups = {};
    let uncategorizedTotal = 0;
    let uncategorizedCount = 0;
    let uncategorizedTransactions = [];
    
    transactions.forEach(transaction => {
      // Only process expenses (negative amounts)
      if (transaction.amount >= 0) return;
      
      // Access the personal_finance group from the groups dictionary
      const personalFinanceGroup = transaction.category?.groups?.personal_finance;
      
      if (!personalFinanceGroup) {
        // Track uncategorized transactions
        uncategorizedTotal += Math.abs(transaction.amount);
        uncategorizedCount += 1;
        uncategorizedTransactions.push(transaction);
        return;
      }
      
      const groupName = personalFinanceGroup.name;
      const groupId = personalFinanceGroup._id;
      
      if (!groupName) return;

      if (!groups[groupName]) {
        groups[groupName] = {
          name: groupName,
          id: groupId,
          total: 0,
          count: 0,
          categories: {}
        };
      }

      groups[groupName].total += Math.abs(transaction.amount);
      groups[groupName].count += 1;

      // Aggregate by category within the group
      const categoryName = transaction.category?.name;
      if (categoryName) {
        if (!groups[groupName].categories[categoryName]) {
          groups[groupName].categories[categoryName] = {
            name: categoryName,
            id: transaction.category._id,
            total: 0,
            count: 0,
            transactions: []
          };
        }
        groups[groupName].categories[categoryName].total += Math.abs(transaction.amount);
        groups[groupName].categories[categoryName].count += 1;
        groups[groupName].categories[categoryName].transactions.push(transaction);
      }
    });

    // Add uncategorized as a group if there are any
    if (uncategorizedCount > 0) {
      groups['Uncategorized'] = {
        name: 'Uncategorized',
        id: 'uncategorized',
        total: uncategorizedTotal,
        count: uncategorizedCount,
        categories: {
          'No category data': {
            name: 'No category data',
            id: 'no-category',
            total: uncategorizedTotal,
            count: uncategorizedCount,
            transactions: uncategorizedTransactions
          }
        }
      };
    }
    
    // Convert to array and sort by total descending
    return Object.values(groups).sort((a, b) => b.total - a.total);
  }, [transactions]);

  const totalSpending = useMemo(() => {
    return groupAggregates.reduce((sum, group) => sum + group.total, 0);
  }, [groupAggregates]);

  const getGroupIcon = (groupName) => {
    const name = groupName.toLowerCase();
    if (name.includes('uncategorized')) return '❓';
    if (name.includes('food') || name.includes('dining')) return '🍔';
    if (name.includes('transport')) return '🚗';
    if (name.includes('utilities')) return '⚡';
    if (name.includes('entertainment')) return '🎬';
    if (name.includes('shopping')) return '🛍️';
    if (name.includes('health')) return '💊';
    if (name.includes('home')) return '🏠';
    if (name.includes('education')) return '📚';
    if (name.includes('personal')) return '👤';
    return '💳';
  };

  const getCategoryIcon = (categoryName) => {
    const name = categoryName.toLowerCase();
    if (name.includes('supermarket') || name.includes('grocery')) return '🛒';
    if (name.includes('restaurant') || name.includes('cafe')) return '☕';
    if (name.includes('telecom') || name.includes('phone')) return '📱';
    if (name.includes('electric') || name.includes('power')) return '💡';
    if (name.includes('gas') || name.includes('fuel')) return '⛽';
    if (name.includes('internet')) return '🌐';
    if (name.includes('water')) return '💧';
    return '📊';
  };

  if (transactions.length === 0) {
    return (
      <div style={{
        maxWidth: '1400px',
        margin: '0 auto',
        padding: '20px'
      }}>
        <div style={{
          backgroundColor: '#fff',
          borderRadius: '12px',
          padding: '60px 40px',
          boxShadow: '0 1px 3px rgba(0, 0, 0, 0.1)',
          textAlign: 'center'
        }}>
          <div style={{
            fontSize: '64px',
            marginBottom: '16px',
            opacity: 0.3
          }}>
            📊
          </div>
          <h3 style={{ 
            fontSize: '20px', 
            fontWeight: '600',
            marginBottom: '8px', 
            color: '#111' 
          }}>
            No Transaction Data
          </h3>
          <p style={{ 
            fontSize: '14px', 
            color: '#6b7280',
            margin: 0
          }}>
            Import transactions from your bank accounts to see spending insights
          </p>
        </div>
      </div>
    );
  }

  return (
    <div style={{
      maxWidth: '1400px',
      margin: '0 auto',
      padding: '20px',
      display: 'flex',
      gap: '24px',
      flexWrap: 'wrap'
    }}>
      {/* Left Panel - Group Summary */}
      <div style={{
        flex: '1 1 400px',
        minWidth: '300px',
        maxWidth: selectedGroup ? '450px' : '100%'
      }}>
        {/* Total Spending Card */}
        <div style={{
          backgroundColor: '#fff',
          borderRadius: '12px',
          padding: '24px',
          marginBottom: '20px',
          boxShadow: '0 1px 3px rgba(0, 0, 0, 0.1)'
        }}>
          <p style={{ 
            fontSize: '14px', 
            color: '#6b7280',
            margin: '0 0 8px 0',
            fontWeight: '500'
          }}>
            Total Spending (Last 31 Days)
          </p>
          <p style={{ 
            fontSize: '32px', 
            fontWeight: '600',
            margin: 0,
            color: '#dc2626'
          }}>
            {formatCurrency(totalSpending)}
          </p>
          <p style={{ 
            fontSize: '13px', 
            color: '#9ca3af',
            margin: '8px 0 0 0'
          }}>
            Across {groupAggregates.length} categories
          </p>
        </div>

        {/* Spending Groups */}
        <div style={{
          backgroundColor: '#fff',
          borderRadius: '12px',
          padding: '20px',
          boxShadow: '0 1px 3px rgba(0, 0, 0, 0.1)'
        }}>
          <h2 style={{
            fontSize: '18px',
            fontWeight: '600',
            color: '#111',
            marginBottom: '16px'
          }}>
            Spending by Category
          </h2>
          
          <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
            {groupAggregates.map((group) => {
              const percentage = (group.total / totalSpending * 100).toFixed(1);
              const isSelected = selectedGroup?.id === group.id;
              
              return (
                <div
                  key={group.id}
                  onClick={() => {
                    if (isSelected) {
                      setSelectedGroup(null);
                      setSelectedCategory(null);
                    } else {
                      setSelectedGroup(group);
                      setSelectedCategory(null);
                    }
                  }}
                  style={{
                    backgroundColor: isSelected ? '#f3f4f6' : '#fff',
                    border: `1px solid ${isSelected ? '#d1d5db' : '#e5e7eb'}`,
                    borderRadius: '8px',
                    padding: '12px 16px',
                    cursor: 'pointer',
                    transition: 'all 0.2s ease'
                  }}
                  onMouseEnter={(e) => {
                    if (!isSelected) {
                      e.currentTarget.style.backgroundColor = '#f9fafb';
                      e.currentTarget.style.borderColor = '#d1d5db';
                    }
                  }}
                  onMouseLeave={(e) => {
                    if (!isSelected) {
                      e.currentTarget.style.backgroundColor = '#fff';
                      e.currentTarget.style.borderColor = '#e5e7eb';
                    }
                  }}
                >
                  <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                    <span style={{ fontSize: '24px' }}>{getGroupIcon(group.name)}</span>
                    
                    <div style={{ flex: 1, minWidth: 0 }}>
                      <div style={{
                        display: 'flex',
                        justifyContent: 'space-between',
                        alignItems: 'baseline',
                        marginBottom: '4px'
                      }}>
                        <span style={{
                          fontSize: '15px',
                          fontWeight: '600',
                          color: '#111'
                        }}>
                          {group.name}
                        </span>
                        <span style={{
                          fontSize: '15px',
                          fontWeight: '600',
                          color: '#dc2626'
                        }}>
                          {formatCurrency(group.total)}
                        </span>
                      </div>
                      
                      <div style={{
                        display: 'flex',
                        justifyContent: 'space-between',
                        alignItems: 'center',
                        gap: '8px'
                      }}>
                        <div style={{
                          flex: 1,
                          height: '4px',
                          backgroundColor: '#e5e7eb',
                          borderRadius: '2px',
                          overflow: 'hidden'
                        }}>
                          <div style={{
                            width: `${percentage}%`,
                            height: '100%',
                            backgroundColor: '#dc2626',
                            transition: 'width 0.3s ease'
                          }} />
                        </div>
                        <span style={{
                          fontSize: '12px',
                          color: '#6b7280',
                          minWidth: '40px',
                          textAlign: 'right'
                        }}>
                          {percentage}%
                        </span>
                      </div>
                      
                      <p style={{
                        fontSize: '12px',
                        color: '#9ca3af',
                        margin: '4px 0 0 0'
                      }}>
                        {group.count} transaction{group.count !== 1 ? 's' : ''}
                      </p>
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </div>

      {/* Right Panel - Category Breakdown */}
      {selectedGroup && (
        <div style={{
          flex: '1 1 600px',
          minWidth: '300px'
        }}>
          <div style={{
            backgroundColor: '#fff',
            borderRadius: '12px',
            padding: '20px',
            boxShadow: '0 1px 3px rgba(0, 0, 0, 0.1)'
          }}>
            <div style={{
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center',
              marginBottom: '20px'
            }}>
              <div>
                <h2 style={{
                  fontSize: '20px',
                  fontWeight: '600',
                  color: '#111',
                  marginBottom: '4px'
                }}>
                  {selectedGroup.name}
                </h2>
                <p style={{
                  fontSize: '14px',
                  color: '#6b7280',
                  margin: 0
                }}>
                  Detailed breakdown
                </p>
              </div>
              <button
                onClick={() => setSelectedGroup(null)}
                style={{
                  padding: '8px 12px',
                  fontSize: '14px',
                  backgroundColor: 'transparent',
                  color: '#6b7280',
                  border: '1px solid #e5e7eb',
                  borderRadius: '6px',
                  cursor: 'pointer',
                  transition: 'all 0.2s'
                }}
                onMouseOver={(e) => {
                  e.currentTarget.style.backgroundColor = '#f3f4f6';
                }}
                onMouseOut={(e) => {
                  e.currentTarget.style.backgroundColor = 'transparent';
                }}
              >
                Close
              </button>
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
              {Object.values(selectedGroup.categories)
                .sort((a, b) => b.total - a.total)
                .map((category) => {
                  const percentage = (category.total / selectedGroup.total * 100).toFixed(1);
                  const isSelected = selectedCategory?.id === category.id;
                  
                  return (
                    <div
                      key={category.id}
                      onClick={() => setSelectedCategory(isSelected ? null : category)}
                      style={{
                        backgroundColor: isSelected ? '#f3f4f6' : '#fff',
                        border: `1px solid ${isSelected ? '#d1d5db' : '#e5e7eb'}`,
                        borderRadius: '8px',
                        padding: '12px 16px',
                        cursor: 'pointer',
                        transition: 'all 0.2s ease'
                      }}
                      onMouseEnter={(e) => {
                        if (!isSelected) {
                          e.currentTarget.style.backgroundColor = '#f9fafb';
                          e.currentTarget.style.borderColor = '#d1d5db';
                        }
                      }}
                      onMouseLeave={(e) => {
                        if (!isSelected) {
                          e.currentTarget.style.backgroundColor = '#fff';
                          e.currentTarget.style.borderColor = '#e5e7eb';
                        }
                      }}
                    >
                      <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                        <span style={{ fontSize: '20px' }}>{getCategoryIcon(category.name)}</span>
                        
                        <div style={{ flex: 1, minWidth: 0 }}>
                          <div style={{
                            display: 'flex',
                            justifyContent: 'space-between',
                            alignItems: 'baseline',
                            marginBottom: '4px'
                          }}>
                            <span style={{
                              fontSize: '14px',
                              fontWeight: '500',
                              color: '#111',
                              overflow: 'hidden',
                              textOverflow: 'ellipsis',
                              whiteSpace: 'nowrap'
                            }}>
                              {category.name}
                            </span>
                            <span style={{
                              fontSize: '14px',
                              fontWeight: '600',
                              color: '#dc2626',
                              marginLeft: '8px'
                            }}>
                              {formatCurrency(category.total)}
                            </span>
                          </div>
                          
                          <div style={{
                            display: 'flex',
                            justifyContent: 'space-between',
                            alignItems: 'center',
                            gap: '8px'
                          }}>
                            <div style={{
                              flex: 1,
                              height: '3px',
                              backgroundColor: '#f3f4f6',
                              borderRadius: '2px',
                              overflow: 'hidden'
                            }}>
                              <div style={{
                                width: `${percentage}%`,
                                height: '100%',
                                backgroundColor: '#f87171',
                                transition: 'width 0.3s ease'
                              }} />
                            </div>
                            <span style={{
                              fontSize: '11px',
                              color: '#9ca3af',
                              minWidth: '35px',
                              textAlign: 'right'
                            }}>
                              {percentage}%
                            </span>
                          </div>
                          
                          <p style={{
                            fontSize: '11px',
                            color: '#9ca3af',
                            margin: '4px 0 0 0'
                          }}>
                            {category.count} transaction{category.count !== 1 ? 's' : ''} • 
                            Avg {formatCurrency(category.total / category.count)}
                          </p>
                        </div>
                      </div>
                    </div>
                  );
                })}
            </div>
          </div>
        </div>
      )}

      {/* Placeholder when no group selected - only on desktop */}
      {!selectedGroup && (
        <div style={{
          flex: '1 1 600px',
          minWidth: '300px'
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
              📊
            </div>
            <h3 style={{ 
              fontSize: '20px', 
              fontWeight: '600',
              marginBottom: '8px', 
              color: '#111' 
            }}>
              Select a Category
            </h3>
            <p style={{ 
              fontSize: '14px', 
              color: '#6b7280',
              margin: 0
            }}>
              Click on a spending category to see detailed breakdown
            </p>
          </div>
        </div>
      )}

      {/* Transaction list when category selected */}
      {selectedGroup && selectedCategory && (
        <div style={{
          flex: '1 1 600px',
          minWidth: '300px'
        }}>
          <div style={{
            backgroundColor: '#fff',
            borderRadius: '12px',
            padding: '20px',
            boxShadow: '0 1px 3px rgba(0, 0, 0, 0.1)',
            maxHeight: 'calc(100vh - 200px)',
            overflow: 'auto'
          }}>
            <div style={{
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center',
              marginBottom: '20px',
              position: 'sticky',
              top: '-20px',
              backgroundColor: '#fff',
              paddingTop: '20px',
              paddingBottom: '12px',
              marginTop: '-20px',
              zIndex: 1
            }}>
              <div>
                <h2 style={{
                  fontSize: '20px',
                  fontWeight: '600',
                  color: '#111',
                  marginBottom: '4px'
                }}>
                  {selectedCategory.name}
                </h2>
                <p style={{
                  fontSize: '14px',
                  color: '#6b7280',
                  margin: 0
                }}>
                  {selectedCategory.count} transaction{selectedCategory.count !== 1 ? 's' : ''} • {formatCurrency(selectedCategory.total)}
                </p>
              </div>
              <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
                {/* Sort Order Toggle */}
                <div style={{
                  display: 'flex',
                  gap: '4px',
                  padding: '4px',
                  backgroundColor: '#f3f4f6',
                  borderRadius: '6px'
                }}>
                  <button
                    onClick={() => setSortOrder('date')}
                    style={{
                      padding: '6px 12px',
                      fontSize: '13px',
                      fontWeight: '500',
                      backgroundColor: sortOrder === 'date' ? '#fff' : 'transparent',
                      color: sortOrder === 'date' ? '#111' : '#6b7280',
                      border: 'none',
                      borderRadius: '4px',
                      cursor: 'pointer',
                      transition: 'all 0.2s',
                      boxShadow: sortOrder === 'date' ? '0 1px 2px rgba(0, 0, 0, 0.05)' : 'none'
                    }}
                  >
                    📅 Date
                  </button>
                  <button
                    onClick={() => setSortOrder('amount')}
                    style={{
                      padding: '6px 12px',
                      fontSize: '13px',
                      fontWeight: '500',
                      backgroundColor: sortOrder === 'amount' ? '#fff' : 'transparent',
                      color: sortOrder === 'amount' ? '#111' : '#6b7280',
                      border: 'none',
                      borderRadius: '4px',
                      cursor: 'pointer',
                      transition: 'all 0.2s',
                      boxShadow: sortOrder === 'amount' ? '0 1px 2px rgba(0, 0, 0, 0.05)' : 'none'
                    }}
                  >
                    💰 Amount
                  </button>
                </div>
                <button
                  onClick={() => setSelectedCategory(null)}
                  style={{
                    padding: '8px 12px',
                    fontSize: '14px',
                    backgroundColor: 'transparent',
                    color: '#6b7280',
                    border: '1px solid #e5e7eb',
                    borderRadius: '6px',
                    cursor: 'pointer',
                    transition: 'all 0.2s'
                  }}
                  onMouseOver={(e) => {
                    e.currentTarget.style.backgroundColor = '#f3f4f6';
                  }}
                  onMouseOut={(e) => {
                    e.currentTarget.style.backgroundColor = 'transparent';
                  }}
                >
                  Close
                </button>
              </div>
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
              {selectedCategory.transactions
                .sort((a, b) => {
                  if (sortOrder === 'date') {
                    return new Date(b.date) - new Date(a.date);
                  } else {
                    return Math.abs(b.amount) - Math.abs(a.amount);
                  }
                })
                .map((transaction) => (
                  <div
                    key={transaction._id}
                    style={{
                      backgroundColor: '#fff',
                      border: '1px solid #e5e7eb',
                      borderRadius: '8px',
                      padding: '12px 16px',
                      display: 'flex',
                      alignItems: 'center',
                      gap: '12px',
                      transition: 'all 0.2s ease'
                    }}
                    onMouseEnter={(e) => {
                      e.currentTarget.style.backgroundColor = '#f9fafb';
                      e.currentTarget.style.boxShadow = '0 2px 4px rgba(0, 0, 0, 0.05)';
                    }}
                    onMouseLeave={(e) => {
                      e.currentTarget.style.backgroundColor = '#fff';
                      e.currentTarget.style.boxShadow = 'none';
                    }}
                  >
                    {/* Merchant Logo */}
                    {transaction.merchant?.logo && (
                      <img
                        src={transaction.merchant.logo}
                        alt={transaction.merchant.name}
                        style={{
                          width: '32px',
                          height: '32px',
                          borderRadius: '6px',
                          objectFit: 'cover'
                        }}
                      />
                    )}
                    {!transaction.merchant?.logo && (
                      <div style={{
                        width: '32px',
                        height: '32px',
                        borderRadius: '6px',
                        backgroundColor: '#f3f4f6',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        fontSize: '16px'
                      }}>
                        💳
                      </div>
                    )}

                    {/* Transaction Details */}
                    <div style={{ flex: 1, minWidth: 0, display: 'flex', alignItems: 'center', gap: '8px' }}>
                      {/* Description */}
                      <span style={{
                        fontSize: '14px',
                        fontWeight: '500',
                        color: '#111',
                        overflow: 'hidden',
                        textOverflow: 'ellipsis',
                        whiteSpace: 'nowrap',
                        flex: '1 1 auto',
                        minWidth: '100px'
                      }}>
                        {transaction.description}
                      </span>

                      {/* Date */}
                      <span style={{
                        fontSize: '12px',
                        color: '#9ca3af',
                        whiteSpace: 'nowrap',
                        flex: '0 0 auto'
                      }}>
                        {formatDate(transaction.date)}
                      </span>
                    </div>

                    {/* Amount */}
                    <span style={{
                      fontSize: '14px',
                      fontWeight: '600',
                      color: transaction.amount < 0 ? '#dc2626' : '#059669',
                      whiteSpace: 'nowrap',
                      flex: '0 0 auto'
                    }}>
                      {formatCurrency(transaction.amount)}
                    </span>
                  </div>
                ))}
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default BudgetingView;
