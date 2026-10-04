export async function api<T>(path: string, init?: RequestInit): Promise<T> {
  const headers = new Headers(init?.headers);
  if (init?.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  const response = await fetch(path, {
    credentials: "include",
    ...init,
    headers
  });

  const data = (await response.json().catch(() => ({}))) as T & { message?: string };
  if (!response.ok) {
    throw Object.assign(new Error(data.message ?? `Request failed (${response.status})`), {
      status: response.status,
      body: data
    });
  }

  return data;
}
