import { http, HttpResponse } from 'msw';
import type { ApiLogListItemDto, PagedResult } from '@/types';
import { activities, apiLogDetail, apiLogItems, authResponse, viewerUser } from './fixtures';
import { workflowHandlers } from './workflow-handlers';

export const API = '/api/v1';

/** Default happy-path handlers; individual tests override with server.use(...). */
export const handlers = [
  http.post(`${API}/auth/login`, async ({ request }) => {
    const body = (await request.json()) as { email: string; password: string };
    if (body.password !== 'Viewer@123') {
      return HttpResponse.json(
        { title: 'Unauthorized', status: 401, detail: 'Invalid credentials' },
        { status: 401, headers: { 'Content-Type': 'application/problem+json' } },
      );
    }
    return HttpResponse.json(authResponse({ ...viewerUser, email: body.email }));
  }),
  http.post(`${API}/auth/refresh`, () => HttpResponse.json(authResponse(viewerUser, '2'))),
  http.post(`${API}/auth/logout`, () => new HttpResponse(null, { status: 204 })),

  http.get(`${API}/activities`, () => HttpResponse.json(activities)),
  http.get(`${API}/auth/me`, () => HttpResponse.json(viewerUser)),

  ...workflowHandlers,

  http.get(`${API}/api-logs`, ({ request }) => {
    const url = new URL(request.url);
    const statusCode = url.searchParams.get('statusCode');
    const method = url.searchParams.get('method');
    const items = apiLogItems.filter(
      (l) => (!statusCode || l.statusCode === Number(statusCode)) && (!method || l.method === method.toUpperCase()),
    );
    const result: PagedResult<ApiLogListItemDto> = {
      items,
      totalCount: items.length,
      page: Number(url.searchParams.get('page') ?? 1),
      pageSize: Number(url.searchParams.get('pageSize') ?? 20),
    };
    return HttpResponse.json(result);
  }),
  http.get(`${API}/api-logs/:id`, ({ params }) => HttpResponse.json(apiLogDetail(Number(params.id)))),
];
