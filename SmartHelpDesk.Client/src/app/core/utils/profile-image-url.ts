import { API_BASE_URL } from '../api-config';

export function resolveProfileImageUrl(
  imageUrl: string | null | undefined
): string | null {
  const value = imageUrl?.trim();

  if (!value) {
    return null;
  }

  // The API may already return a complete URL.
  if (/^https?:\/\//i.test(value)) {
    return value;
  }

  // Convert a relative image path into an API-origin URL.
  const apiOrigin = API_BASE_URL.replace(/\/api\/?$/i, '');
  const path = value.replace(/^\/+/, '');

  return `${apiOrigin}/${path}`;
}