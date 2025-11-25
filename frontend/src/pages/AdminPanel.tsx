import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { motion } from 'framer-motion';
import { ArrowUpDown, ArrowUp, ArrowDown, Eye } from 'lucide-react';
import { adminApi, Journey, User } from '../services/api';
import { extractErrorMessages } from '../utils/errorHandler';
import { useConfirmDialog } from '../contexts/ConfirmDialogContext';
import toast from 'react-hot-toast';
import './AdminPanel.css';

type TabType = 'stats' | 'journeys' | 'monthly' | 'users';

const AdminPanel = () => {
  const queryClient = useQueryClient();
  const confirmDialog = useConfirmDialog();
  const [activeTab, setActiveTab] = useState<TabType>('stats');
  const [journeyPage, setJourneyPage] = useState(1);
  const [monthlyPage, setMonthlyPage] = useState(1);
  const [sortConfig, setSortConfig] = useState<{ orderBy?: string; direction?: 'asc' | 'desc' }>(
    {}
  );

  const [filters, setFilters] = useState({
    userId: '',
    transportType: '',
    startDateFrom: '',
    startDateTo: '',
    arrivalDateFrom: '',
    arrivalDateTo: '',
    minDistance: '',
    maxDistance: '',
  });

  const { data: stats, isLoading: statsLoading } = useQuery('admin-stats', () =>
    adminApi.getStatistics()
  );

  const { data: journeysData, isLoading: journeysLoading } = useQuery(
    ['admin-journeys', journeyPage, filters, sortConfig],
    () =>
      adminApi.getAllJourneys(journeyPage, 20, {
        userId: filters.userId || undefined,
        transportType: filters.transportType || undefined,
        startDateFrom: filters.startDateFrom || undefined,
        startDateTo: filters.startDateTo || undefined,
        arrivalDateFrom: filters.arrivalDateFrom || undefined,
        arrivalDateTo: filters.arrivalDateTo || undefined,
        minDistance: filters.minDistance ? parseFloat(filters.minDistance) : undefined,
        maxDistance: filters.maxDistance ? parseFloat(filters.maxDistance) : undefined,
        orderBy: sortConfig.orderBy,
        direction: sortConfig.direction,
      })
  );

  const { data: monthlyData, isLoading: monthlyLoading } = useQuery(
    ['admin-monthly', monthlyPage, sortConfig],
    () =>
      adminApi.getMonthlyDistance(
        monthlyPage,
        20,
        sortConfig.orderBy === 'TotalDistanceKm' ? 'TotalDistanceKm' : 'UserId',
        sortConfig.direction
      )
  );

  const { data: usersData, isLoading: usersLoading } = useQuery(
    'admin-users',
    () => adminApi.getUsers(),
    {
      enabled: activeTab === 'users' || activeTab === 'journeys' || activeTab === 'monthly', // Fetch users for name lookup
    }
  );

  const updateStatusMutation = useMutation(
    ({ userId, status }: { userId: string; status: 'Active' | 'Suspended' }) =>
      adminApi.updateUserStatus(userId, status),
    {
      onSuccess: () => {
        toast.success('User status updated successfully');
        queryClient.invalidateQueries('admin-users');
      },
      onError: (error) => {
        const { message } = extractErrorMessages(error);
        toast.error(message);
      },
    }
  );

  const handleFilterChange = (key: string, value: string) => {
    setFilters((prev: typeof filters) => ({ ...prev, [key]: value }));
    setJourneyPage(1);
  };

  const clearFilters = () => {
    setFilters({
      userId: '',
      transportType: '',
      startDateFrom: '',
      startDateTo: '',
      arrivalDateFrom: '',
      arrivalDateTo: '',
      minDistance: '',
      maxDistance: '',
    });
    setJourneyPage(1);
    setSortConfig({});
  };

  const handleSort = (field: string) => {
    setSortConfig((prev: typeof sortConfig) => {
      if (prev.orderBy === field) {
        return {
          orderBy: field,
          direction: prev.direction === 'asc' ? 'desc' : 'asc',
        };
      }
      return { orderBy: field, direction: 'asc' };
    });
    setJourneyPage(1);
  };

  const handleMonthlySort = (field: 'UserId' | 'TotalDistanceKm') => {
    setSortConfig({ orderBy: field, direction: sortConfig.direction === 'asc' ? 'desc' : 'asc' });
    setMonthlyPage(1);
  };

  const handleStatusChange = async (userId: string, newStatus: 'Active' | 'Suspended') => {
    const confirmed = await confirmDialog.confirm({
      title: 'Change User Status',
      message: `Are you sure you want to change this user's status to ${newStatus}?`,
      variant: 'warning',
      confirmText: 'Change',
    });
    if (confirmed) {
      updateStatusMutation.mutate({ userId, status: newStatus });
    }
  };

  const journeys = journeysData?.data.items || [];
  const totalPages = Math.ceil((journeysData?.data.totalCount || 0) / 20);
  const monthlyStats = Array.isArray(monthlyData?.data) ? monthlyData.data : [];
  const hasMoreMonthly = monthlyStats.length === 20;

  return (
    <div className="page-container admin-panel">
      <div className="page-header">
        <h1>Admin Panel</h1>
      </div>

      <div className="admin-tabs">
        <button
          className={`admin-tab ${activeTab === 'stats' ? 'active' : ''}`}
          onClick={() => setActiveTab('stats')}
        >
          Statistics
        </button>
        <button
          className={`admin-tab ${activeTab === 'journeys' ? 'active' : ''}`}
          onClick={() => setActiveTab('journeys')}
        >
          All Journeys
        </button>
        <button
          className={`admin-tab ${activeTab === 'monthly' ? 'active' : ''}`}
          onClick={() => setActiveTab('monthly')}
        >
          Monthly Distance
        </button>
        <button
          className={`admin-tab ${activeTab === 'users' ? 'active' : ''}`}
          onClick={() => setActiveTab('users')}
        >
          User Management
        </button>
      </div>

      {activeTab === 'stats' && (
        <motion.div
          className="admin-content"
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
        >
          {statsLoading ? (
            <div className="loading">Loading statistics...</div>
          ) : stats?.data ? (
            <div className="stats-grid">
              <div className="stat-card">
                <div className="stat-label">Total Journeys</div>
                <div className="stat-value">{stats.data.totalJourneys}</div>
              </div>
              <div className="stat-card">
                <div className="stat-label">Total Users</div>
                <div className="stat-value">{stats.data.totalUsers}</div>
              </div>
              <div className="stat-card">
                <div className="stat-label">Total Distance</div>
                <div className="stat-value">{stats.data.totalDistanceKm.toFixed(2)} km</div>
              </div>
              <div className="stat-card">
                <div className="stat-label">Average Distance</div>
                <div className="stat-value">{stats.data.averageDistanceKm.toFixed(2)} km</div>
              </div>
            </div>
          ) : null}
        </motion.div>
      )}

      {activeTab === 'journeys' && (
        <motion.div
          className="admin-content"
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
        >
          <div className="card">
            <div className="filters-section">
              <h3>Filters & Sorting</h3>
              <div className="filters-grid">
                <div className="form-group">
                  <label>User ID</label>
                  <input
                    type="text"
                    value={filters.userId}
                    onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                      handleFilterChange('userId', e.target.value)
                    }
                    placeholder="Filter by user ID"
                  />
                </div>
                <div className="form-group">
                  <label>Transport Type</label>
                  <select
                    value={filters.transportType}
                    onChange={(e: React.ChangeEvent<HTMLSelectElement>) =>
                      handleFilterChange('transportType', e.target.value)
                    }
                  >
                    <option value="">All Types</option>
                    <option value="Commercial">Commercial</option>
                    <option value="Cargo">Cargo</option>
                    <option value="Private">Private</option>
                    <option value="Charter">Charter</option>
                    <option value="Military">Military</option>
                    <option value="Helicopter">Helicopter</option>
                    <option value="Other">Other</option>
                  </select>
                </div>
                <div className="form-group">
                  <label>Start Date From</label>
                  <input
                    type="date"
                    value={filters.startDateFrom}
                    onChange={(e) => handleFilterChange('startDateFrom', e.target.value)}
                  />
                </div>
                <div className="form-group">
                  <label>Start Date To</label>
                  <input
                    type="date"
                    value={filters.startDateTo}
                    onChange={(e) => handleFilterChange('startDateTo', e.target.value)}
                  />
                </div>
                <div className="form-group">
                  <label>Arrival Date From</label>
                  <input
                    type="date"
                    value={filters.arrivalDateFrom}
                    onChange={(e) => handleFilterChange('arrivalDateFrom', e.target.value)}
                  />
                </div>
                <div className="form-group">
                  <label>Arrival Date To</label>
                  <input
                    type="date"
                    value={filters.arrivalDateTo}
                    onChange={(e) => handleFilterChange('arrivalDateTo', e.target.value)}
                  />
                </div>
                <div className="form-group">
                  <label>Min Distance (km)</label>
                  <input
                    type="number"
                    step="0.1"
                    value={filters.minDistance}
                    onChange={(e) => handleFilterChange('minDistance', e.target.value)}
                    placeholder="0"
                  />
                </div>
                <div className="form-group">
                  <label>Max Distance (km)</label>
                  <input
                    type="number"
                    step="0.1"
                    value={filters.maxDistance}
                    onChange={(e) => handleFilterChange('maxDistance', e.target.value)}
                    placeholder="∞"
                  />
                </div>
              </div>
              <div className="filters-actions">
                <button onClick={clearFilters} className="btn btn-secondary btn-sm">
                  Clear Filters
                </button>
              </div>
            </div>
          </div>

          {journeysLoading ? (
            <div className="loading">Loading journeys...</div>
          ) : journeys.length === 0 ? (
            <div className="card empty-state">
              <p>No journeys found matching the filters.</p>
            </div>
          ) : (
            <>
              <div className="card">
                <div className="table-container">
                  <table className="table">
                    <thead>
                      <tr>
                        <th>
                          <button className="sortable-header" onClick={() => handleSort('UserId')}>
                            User
                            {sortConfig.orderBy === 'UserId' &&
                              (sortConfig.direction === 'asc' ? (
                                <ArrowUp size={14} />
                              ) : (
                                <ArrowDown size={14} />
                              ))}
                            {!sortConfig.orderBy && <ArrowUpDown size={14} />}
                          </button>
                        </th>
                        <th>Route</th>
                        <th>
                          <button
                            className="sortable-header"
                            onClick={() => handleSort('TransportType')}
                          >
                            Transport
                            {sortConfig.orderBy === 'TransportType' &&
                              (sortConfig.direction === 'asc' ? (
                                <ArrowUp size={14} />
                              ) : (
                                <ArrowDown size={14} />
                              ))}
                            {!sortConfig.orderBy && <ArrowUpDown size={14} />}
                          </button>
                        </th>
                        <th>
                          <button
                            className="sortable-header"
                            onClick={() => handleSort('DistanceKm')}
                          >
                            Distance
                            {sortConfig.orderBy === 'DistanceKm' &&
                              (sortConfig.direction === 'asc' ? (
                                <ArrowUp size={14} />
                              ) : (
                                <ArrowDown size={14} />
                              ))}
                            {!sortConfig.orderBy && <ArrowUpDown size={14} />}
                          </button>
                        </th>
                        <th>
                          <button
                            className="sortable-header"
                            onClick={() => handleSort('StartTime')}
                          >
                            Start Time
                            {sortConfig.orderBy === 'StartTime' &&
                              (sortConfig.direction === 'asc' ? (
                                <ArrowUp size={14} />
                              ) : (
                                <ArrowDown size={14} />
                              ))}
                            {!sortConfig.orderBy && <ArrowUpDown size={14} />}
                          </button>
                        </th>
                        <th>Actions</th>
                      </tr>
                    </thead>
                    <tbody>
                      {journeys.map((journey: Journey) => {
                        const user = usersData?.data?.find(
                          (u: User) => u.userId === journey.userId
                        );
                        const userName = user
                          ? user.name ||
                            user.username ||
                            user.email ||
                            journey.userId.substring(0, 8)
                          : journey.userId.substring(0, 8);
                        return (
                          <tr key={journey.id}>
                            <td>
                              <span className="user-name">{userName}</span>
                            </td>
                            <td>
                              <div className="route-cell">
                                {journey.startLocation} → {journey.arrivalLocation}
                              </div>
                            </td>
                            <td>
                              <span className="badge badge-info">
                                {journey.transportType || 'N/A'}
                              </span>
                            </td>
                            <td>{journey.distanceKm} km</td>
                            <td>
                              {new Date(journey.startTime).toLocaleDateString('en-US', {
                                month: 'short',
                                day: 'numeric',
                                year: 'numeric',
                                hour: '2-digit',
                                minute: '2-digit',
                              })}
                            </td>
                            <td>
                              <div className="table-actions">
                                <button
                                  onClick={() =>
                                    window.open(`/admin/journeys/${journey.id}`, '_blank')
                                  }
                                  className="btn btn-icon btn-secondary"
                                  title="View"
                                >
                                  <Eye size={16} />
                                </button>
                              </div>
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              </div>

              {totalPages > 1 && (
                <div className="pagination">
                  <button
                    className="btn btn-secondary"
                    onClick={() => setJourneyPage((p: number) => Math.max(1, p - 1))}
                    disabled={journeyPage === 1}
                  >
                    Previous
                  </button>
                  <span className="pagination-info">
                    Page {journeyPage} of {totalPages} ({journeysData?.data.totalCount || 0} total)
                  </span>
                  <button
                    className="btn btn-secondary"
                    onClick={() => setJourneyPage((p: number) => Math.min(totalPages, p + 1))}
                    disabled={journeyPage === totalPages}
                  >
                    Next
                  </button>
                </div>
              )}
            </>
          )}
        </motion.div>
      )}

      {activeTab === 'monthly' && (
        <motion.div
          className="admin-content"
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
        >
          <div className="card">
            <div className="section-header">
              <h3>Monthly Distance Statistics</h3>
              <p className="text-muted">Total distance travelled per user per calendar month</p>
            </div>
          </div>

          {monthlyLoading ? (
            <div className="loading">Loading monthly statistics...</div>
          ) : monthlyStats.length === 0 ? (
            <div className="card empty-state">
              <p>No monthly distance data available.</p>
            </div>
          ) : (
            <>
              <div className="card">
                <div className="table-container">
                  <table className="table">
                    <thead>
                      <tr>
                        <th>
                          <button
                            className="sortable-header"
                            onClick={() => handleMonthlySort('UserId')}
                          >
                            User
                            {sortConfig.orderBy === 'UserId' &&
                              (sortConfig.direction === 'asc' ? (
                                <ArrowUp size={14} />
                              ) : (
                                <ArrowDown size={14} />
                              ))}
                            {sortConfig.orderBy !== 'UserId' && <ArrowUpDown size={14} />}
                          </button>
                        </th>
                        <th>Year</th>
                        <th>Month</th>
                        <th>
                          <button
                            className="sortable-header"
                            onClick={() => handleMonthlySort('TotalDistanceKm')}
                          >
                            Total Distance (km)
                            {sortConfig.orderBy === 'TotalDistanceKm' &&
                              (sortConfig.direction === 'asc' ? (
                                <ArrowUp size={14} />
                              ) : (
                                <ArrowDown size={14} />
                              ))}
                            {sortConfig.orderBy !== 'TotalDistanceKm' && <ArrowUpDown size={14} />}
                          </button>
                        </th>
                      </tr>
                    </thead>
                    <tbody>
                      {monthlyStats.map(
                        (
                          stat: {
                            userId: string;
                            year: number;
                            month: number;
                            totalDistanceKm: number;
                          },
                          index: number
                        ) => {
                          const user = usersData?.data?.find((u: User) => u.userId === stat.userId);
                          const userName = user
                            ? user.name ||
                              user.username ||
                              user.email ||
                              stat.userId?.substring(0, 8)
                            : stat.userId?.substring(0, 8) || 'N/A';
                          const monthName = stat.month
                            ? new Date(2000, stat.month - 1).toLocaleString('default', {
                                month: 'long',
                              })
                            : 'N/A';
                          return (
                            <tr key={index}>
                              <td>
                                <span className="user-name">{userName}</span>
                              </td>
                              <td>{stat.year || 'N/A'}</td>
                              <td>{monthName}</td>
                              <td>
                                <strong>{stat.totalDistanceKm?.toFixed(2) || '0.00'} km</strong>
                              </td>
                            </tr>
                          );
                        }
                      )}
                    </tbody>
                  </table>
                </div>
              </div>

              {(monthlyPage > 1 || hasMoreMonthly) && (
                <div className="pagination">
                  <button
                    className="btn btn-secondary"
                    onClick={() => setMonthlyPage((p: number) => Math.max(1, p - 1))}
                    disabled={monthlyPage === 1}
                  >
                    Previous
                  </button>
                  <span className="pagination-info">
                    Page {monthlyPage} {hasMoreMonthly && '(more available)'}
                  </span>
                  <button
                    className="btn btn-secondary"
                    onClick={() => setMonthlyPage((p: number) => p + 1)}
                    disabled={!hasMoreMonthly}
                  >
                    Next
                  </button>
                </div>
              )}
            </>
          )}
        </motion.div>
      )}

      {activeTab === 'users' && (
        <motion.div
          className="admin-content"
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
        >
          {usersLoading ? (
            <div className="loading">Loading users...</div>
          ) : !usersData?.data || (Array.isArray(usersData.data) && usersData.data.length === 0) ? (
            <div className="card empty-state">
              <p>No users found.</p>
            </div>
          ) : (
            <div className="card">
              <div className="section-header">
                <h3>User Management</h3>
                <p className="text-muted">Manage user account status</p>
              </div>
              <div className="table-container">
                <table className="table">
                  <thead>
                    <tr>
                      <th>Name</th>
                      <th>Email</th>
                      <th>Username</th>
                      <th>Status</th>
                      <th>Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {(Array.isArray(usersData?.data) ? usersData.data : []).map(
                      (user: User & { status?: string }) => {
                        const rawStatus = user.status || 'Active';
                        const currentStatus = rawStatus === 'Deactivated' ? 'Suspended' : rawStatus;
                        const getStatusBadgeClass = (status: string) => {
                          switch (status) {
                            case 'Active':
                              return 'badge-success';
                            case 'Suspended':
                              return 'badge-warning';
                            default:
                              return 'badge-info';
                          }
                        };

                        return (
                          <tr key={user.userId}>
                            <td>{user.name || user.username || 'N/A'}</td>
                            <td>{user.email || 'N/A'}</td>
                            <td>{user.username || 'N/A'}</td>
                            <td>
                              <span className={`badge ${getStatusBadgeClass(currentStatus)}`}>
                                {currentStatus}
                              </span>
                            </td>
                            <td>
                              <div className="status-actions">
                                <select
                                  className="status-select"
                                  value={currentStatus}
                                  onChange={(e: React.ChangeEvent<HTMLSelectElement>) =>
                                    handleStatusChange(
                                      user.userId,
                                      e.target.value as 'Active' | 'Suspended'
                                    )
                                  }
                                  disabled={updateStatusMutation.isLoading}
                                >
                                  <option value="Active">Active</option>
                                  <option value="Suspended">Suspended</option>
                                </select>
                              </div>
                            </td>
                          </tr>
                        );
                      }
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </motion.div>
      )}
    </div>
  );
};

export default AdminPanel;
