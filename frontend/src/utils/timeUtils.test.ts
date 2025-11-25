import { describe, it, expect } from 'vitest';
import {
  calculateDuration,
  formatDate,
  getRelativeTime,
  extractAirportCode,
  formatFlightTime,
  formatFlightDate,
} from './timeUtils';

describe('timeUtils', () => {
  describe('calculateDuration', () => {
    it('should return minutes for durations under 1 hour', () => {
      const start = new Date('2024-01-01T10:00:00');
      const end = new Date('2024-01-01T10:45:00');
      expect(calculateDuration(start, end)).toBe('45m');
    });

    it('should return hours and minutes for durations under 24 hours', () => {
      const start = new Date('2024-01-01T10:00:00');
      const end = new Date('2024-01-01T12:30:00');
      expect(calculateDuration(start, end)).toBe('2h 30m');
    });

    it('should return only hours when minutes are zero', () => {
      const start = new Date('2024-01-01T10:00:00');
      const end = new Date('2024-01-01T13:00:00');
      expect(calculateDuration(start, end)).toBe('3h');
    });

    it('should return days and hours for durations over 24 hours', () => {
      const start = new Date('2024-01-01T10:00:00');
      const end = new Date('2024-01-03T14:00:00');
      expect(calculateDuration(start, end)).toBe('2d 4h');
    });

    it('should return only days when hours are zero', () => {
      const start = new Date('2024-01-01T10:00:00');
      const end = new Date('2024-01-04T10:00:00');
      expect(calculateDuration(start, end)).toBe('3d');
    });

    it('should handle string dates', () => {
      const result = calculateDuration('2024-01-01T10:00:00', '2024-01-01T11:30:00');
      expect(result).toBe('1h 30m');
    });
  });

  describe('formatDate', () => {
    it('should format date with default format', () => {
      const date = new Date('2024-01-15T14:30:00');
      const result = formatDate(date);
      expect(result).toMatch(/Jan/); // Contains month abbreviation
    });

    it('should format date with custom format', () => {
      const date = new Date('2024-01-15T14:30:00');
      const result = formatDate(date, 'yyyy-MM-dd');
      expect(result).toBe('2024-01-15');
    });

    it('should handle string dates', () => {
      const result = formatDate('2024-01-15T14:30:00', 'yyyy-MM-dd');
      expect(result).toBe('2024-01-15');
    });
  });

  describe('getRelativeTime', () => {
    it('should return relative time string', () => {
      const pastDate = new Date(Date.now() - 2 * 60 * 60 * 1000); // 2 hours ago
      const result = getRelativeTime(pastDate);
      expect(result).toContain('ago');
    });

    it('should handle string dates', () => {
      const pastDate = new Date(Date.now() - 1000 * 60 * 60); // 1 hour ago
      const result = getRelativeTime(pastDate.toISOString());
      expect(result).toContain('ago');
    });
  });

  describe('extractAirportCode', () => {
    it('should extract code from parentheses format', () => {
      expect(extractAirportCode('New York (JFK)')).toBe('JFK');
      expect(extractAirportCode('London (LHR)')).toBe('LHR');
    });

    it('should extract code from space-separated format', () => {
      expect(extractAirportCode('New York JFK')).toBe('JFK');
      expect(extractAirportCode('London LHR')).toBe('LHR');
    });

    it('should extract first 3 uppercase characters', () => {
      expect(extractAirportCode('JFK Airport')).toBe('JFK');
      expect(extractAirportCode('LAXInternational')).toBe('LAX');
    });

    it('should fallback to first 3 characters uppercase', () => {
      expect(extractAirportCode('rome')).toBe('ROM');
      expect(extractAirportCode('paris')).toBe('PAR');
    });

    it('should handle short strings', () => {
      expect(extractAirportCode('NY')).toBe('NY');
    });
  });

  describe('formatFlightTime', () => {
    it('should format time in HH:mm format', () => {
      const date = new Date('2024-01-15T14:30:00');
      expect(formatFlightTime(date)).toBe('14:30');
    });

    it('should handle string dates', () => {
      expect(formatFlightTime('2024-01-15T09:05:00')).toBe('09:05');
    });

    it('should pad single digits', () => {
      const date = new Date('2024-01-15T03:05:00');
      expect(formatFlightTime(date)).toBe('03:05');
    });
  });

  describe('formatFlightDate', () => {
    it('should format date in dd/MM/yyyy format', () => {
      const date = new Date('2024-01-15T14:30:00');
      expect(formatFlightDate(date)).toBe('15/01/2024');
    });

    it('should handle string dates', () => {
      expect(formatFlightDate('2024-12-25T14:30:00')).toBe('25/12/2024');
    });

    it('should pad single digits', () => {
      const date = new Date('2024-03-05T14:30:00');
      expect(formatFlightDate(date)).toBe('05/03/2024');
    });
  });
});
