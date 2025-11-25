import { differenceInMinutes, format, formatDistanceToNow } from 'date-fns';

/**
 * Calculates the duration between two dates in a human-readable format.
 * @param startTime - The start date/time.
 * @param endTime - The end date/time.
 * @returns A formatted duration string (e.g., "2h 30m", "3d", "45m").
 */
export const calculateDuration = (startTime: string | Date, endTime: string | Date): string => {
  const start = new Date(startTime);
  const end = new Date(endTime);
  const minutes = differenceInMinutes(end, start);

  if (minutes < 60) {
    return `${minutes}m`;
  }

  const hours = Math.floor(minutes / 60);
  const remainingMinutes = minutes % 60;

  if (hours < 24) {
    return remainingMinutes > 0 ? `${hours}h ${remainingMinutes}m` : `${hours}h`;
  }

  const days = Math.floor(hours / 24);
  const remainingHours = hours % 24;
  return remainingHours > 0 ? `${days}d ${remainingHours}h` : `${days}d`;
};

/**
 * Formats a date for display using the specified format string.
 * @param date - The date to format.
 * @param formatStr - The format string (default: 'PPp').
 * @returns The formatted date string.
 */
export const formatDate = (date: string | Date, formatStr: string = 'PPp'): string => {
  return format(new Date(date), formatStr);
};

/**
 * Gets a relative time string (e.g., "2 hours ago").
 * @param date - The date to get relative time for.
 * @returns A relative time string.
 */
export const getRelativeTime = (date: string | Date): string => {
  return formatDistanceToNow(new Date(date), { addSuffix: true });
};

/**
 * Extracts an airport code from a location string.
 * Supports patterns like "City (CODE)" or "City CODE".
 * @param location - The location string.
 * @returns The extracted airport code or first 3 uppercase characters.
 */
export const extractAirportCode = (location: string): string => {
  const matchParentheses = location.match(/\(([A-Z]{3})\)/);
  if (matchParentheses) {
    return matchParentheses[1];
  }

  const matchSpace = location.match(/\b([A-Z]{3})\b/);
  if (matchSpace) {
    return matchSpace[1];
  }

  const uppercase = location.replace(/[^A-Z]/g, '');
  if (uppercase.length >= 3) {
    return uppercase.substring(0, 3);
  }

  return location.substring(0, 3).toUpperCase();
};

/**
 * Formats a time for flight display (HH:MM format).
 * @param date - The date/time to format.
 * @returns The formatted time string (e.g., "14:30").
 */
export const formatFlightTime = (date: string | Date): string => {
  return format(new Date(date), 'HH:mm');
};

/**
 * Formats a date for flight display (dd/MM/yyyy format).
 * @param date - The date to format.
 * @returns The formatted date string (e.g., "15/01/2024").
 */
export const formatFlightDate = (date: string | Date): string => {
  return format(new Date(date), 'dd/MM/yyyy');
};
