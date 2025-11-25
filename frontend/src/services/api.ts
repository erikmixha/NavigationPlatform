import axios from 'axios';

const api = axios.create({
  baseURL: '/api',
  withCredentials: true,
});

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (
      error.response?.status === 401 &&
      !window.location.pathname.includes('/login') &&
      !window.location.pathname.includes('/api/auth/login') &&
      !window.location.pathname.includes('/signin-oidc') &&
      !window.location.search.includes('error=')
    ) {
      const hasAuthCookie = document.cookie.includes('.NavPlat.Auth=');
      if (!hasAuthCookie) {
        const returnUrl = encodeURIComponent(window.location.pathname + window.location.search);
        window.location.href = `/api/auth/login?returnUrl=${returnUrl}`;
      }
    }
    return Promise.reject(error);
  }
);

/**
 * Represents a journey in the system.
 */
export interface Journey {
  id: string;
  userId: string;
  startLocation: string;
  startTime: string;
  arrivalLocation: string;
  arrivalTime: string;
  transportType: string;
  distanceKm: number;
  isFavorite: boolean;
  isShared?: boolean;
}

export type CreateJourneyRequest = Omit<Journey, 'id' | 'userId' | 'isFavorite'>;
export type UpdateJourneyRequest = Omit<Journey, 'id' | 'userId' | 'isFavorite'>;

/**
 * Represents a paginated result.
 */
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

/**
 * API client for journey-related operations.
 */
export const journeyApi = {
  /**
   * Gets paginated journeys for the current user.
   */
  getAll: (
    page: number = 1,
    pageSize: number = 20,
    startDateFrom?: string,
    startDateTo?: string
  ) => {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    });
    if (startDateFrom) params.append('startDateFrom', startDateFrom);
    if (startDateTo) params.append('startDateTo', startDateTo);
    return api.get<PagedResult<Journey>>(`/journeys?${params.toString()}`);
  },

  /**
   * Gets a journey by its identifier.
   */
  getById: (id: string) => api.get<Journey>(`/journeys/${id}`),

  /**
   * Creates a new journey.
   */
  create: (data: CreateJourneyRequest) => api.post<{ id: string }>('/journeys', data),

  /**
   * Updates an existing journey.
   */
  update: (id: string, data: UpdateJourneyRequest) => api.put(`/journeys/${id}`, data),

  /**
   * Deletes a journey.
   */
  delete: (id: string) => api.delete(`/journeys/${id}`),

  /**
   * Adds a journey to favorites.
   */
  addFavorite: (id: string) => api.post(`/journeys/${id}/favorite`),

  /**
   * Removes a journey from favorites.
   */
  removeFavorite: (id: string) => api.delete(`/journeys/${id}/favorite`),

  /**
   * Shares a journey with one or more users.
   */
  share: (id: string, userIds: string[]) =>
    api.post(`/journeys/${id}/share`, { sharedWithUserIds: userIds }),

  /**
   * Gets the list of users a journey is shared with.
   */
  getSharedUsers: (id: string) => api.get<string[]>(`/journeys/${id}/share`),

  /**
   * Unshares a journey from a specific user.
   */
  unshare: (id: string, userId: string) => api.delete(`/journeys/${id}/share/${userId}`),

  /**
   * Generates a public link for a journey.
   */
  generatePublicLink: (id: string) =>
    api.post<{ token: string; url: string }>(`/journeys/${id}/public-link`),

  /**
   * Revokes the public link for a journey.
   */
  revokePublicLink: (id: string) => api.delete(`/journeys/${id}/public-link`),

  /**
   * Gets a journey by its public link token (anonymous access).
   */
  getByPublicLink: (token: string) =>
    api.get<Journey>(`/journeys/public/${token}`, {
      withCredentials: false,
    }),
};

/**
 * Represents a user in the system.
 */
export interface User {
  userId: string;
  email: string;
  name: string;
  username: string;
}

/**
 * API client for authentication-related operations.
 */
export const authApi = {
  /**
   * Gets the current authenticated user.
   */
  getCurrentUser: () => api.get('/auth/user'),

  /**
   * Gets all users (for sharing purposes).
   */
  getUsers: () => api.get<User[]>('/auth/users'),

  /**
   * Logs out the current user.
   */
  logout: () => api.post('/auth/logout'),
};

/**
 * API client for admin operations.
 */
export const adminApi = {
  /**
   * Gets overall statistics.
   */
  getStatistics: () => api.get('/admin/statistics'),

  /**
   * Gets all users with their status.
   */
  getUsers: () =>
    api.get<
      Array<{ userId: string; username: string; email: string; name: string; status: string }>
    >('/admin/users'),

  /**
   * Gets all journeys with pagination and filtering.
   */
  getAllJourneys: (
    page: number = 1,
    pageSize: number = 20,
    filters?: {
      userId?: string;
      transportType?: string;
      startDateFrom?: string;
      startDateTo?: string;
      arrivalDateFrom?: string;
      arrivalDateTo?: string;
      minDistance?: number;
      maxDistance?: number;
      orderBy?: string;
      direction?: string;
    }
  ) => {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    });
    if (filters?.userId) params.append('userId', filters.userId);
    if (filters?.transportType) params.append('transportType', filters.transportType);
    if (filters?.startDateFrom) params.append('startDateFrom', filters.startDateFrom);
    if (filters?.startDateTo) params.append('startDateTo', filters.startDateTo);
    if (filters?.arrivalDateFrom) params.append('arrivalDateFrom', filters.arrivalDateFrom);
    if (filters?.arrivalDateTo) params.append('arrivalDateTo', filters.arrivalDateTo);
    if (filters?.minDistance !== undefined)
      params.append('minDistance', filters.minDistance.toString());
    if (filters?.maxDistance !== undefined)
      params.append('maxDistance', filters.maxDistance.toString());
    if (filters?.orderBy) params.append('orderBy', filters.orderBy);
    if (filters?.direction) params.append('direction', filters.direction);
    return api.get<PagedResult<Journey>>(`/admin/journeys?${params.toString()}`);
  },

  /**
   * Gets all journeys for a specific user.
   */
  getUserJourneys: (userId: string, page: number = 1, pageSize: number = 20) =>
    api.get<PagedResult<Journey>>(
      `/admin/users/${userId}/journeys?page=${page}&pageSize=${pageSize}`
    ),

  /**
   * Gets monthly distance statistics.
   */
  getMonthlyDistance: (
    page: number = 1,
    pageSize: number = 20,
    orderBy?: string,
    direction?: string
  ) => {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    });
    if (orderBy) params.append('orderBy', orderBy);
    if (direction) params.append('direction', direction);
    return api.get(`/admin/statistics/monthly-distance?${params.toString()}`);
  },

  /**
   * Updates a user's status (Active or Suspended).
   */
  updateUserStatus: (userId: string, status: 'Active' | 'Suspended') =>
    api.patch(`/admin/users/${userId}/status`, { status }),
};

export default api;
