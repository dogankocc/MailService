import axios from 'axios'
import { useAuthStore } from '../store/authStore'

const api = axios.create({
  baseURL: '',
  headers: { 'Content-Type': 'application/json' },
})

api.interceptors.request.use((config) => {
  const token = useAuthStore.getState().token
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      useAuthStore.getState().logout()
      window.location.href = '/login'
    }
    return Promise.reject(error)
  }
)

export interface OAuthClient {
  id: number
  clientId: string
  name: string
  scopes: string
  isActive: boolean
  createdDate: string
}

export interface CreateOAuthClient {
  clientId: string
  name: string
  scopes: string
  clientSecret: string
}

export interface MailTemplate {
  id: number
  code: string
  name: string
  subject: string
  body: string
  version: number
  isActive: boolean
  createdDate: string
}

export interface CreateTemplate {
  code: string
  name: string
  subject: string
  body: string
}

export interface MailLogStatus {
  requestId: string
  total: number
  successCount: number
  failedCount: number
  queuedCount: number
  recipients: Array<{
    to: string
    subject: string
    status: string
    sentDate: string
  }>
}

export interface ClientApplication {
  id: number
  clientId: string
  name: string
  dailyLimit: number
  minuteLimit: number
  isActive: boolean
  createdDate: string
}

export interface CreateClientApp {
  clientId: string
  name: string
  dailyLimit: number
  minuteLimit: number
}

export const authApi = {
  login: async (clientId: string, clientSecret: string) => {
    const form = new URLSearchParams()
    form.append('grantType', 'client_credentials')
    form.append('clientId', clientId)
    form.append('clientSecret', clientSecret)
    form.append('scope', 'mail.admin mail.template.read mail.template.write')

    const response = await axios.post('/token/connect/token', form, {
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    })
    return response.data
  },
}

export const clientsApi = {
  getAll: () => api.get<OAuthClient[]>('/api/oauthclients'),
  getById: (id: string) => api.get<OAuthClient>(`/api/oauthclients/${id}`),
  create: (data: CreateOAuthClient) => api.post('/api/oauthclients', data),
  update: (id: string, data: CreateOAuthClient) => api.put(`/api/oauthclients/${id}`, data),
  delete: (id: string) => api.delete(`/api/oauthclients/${id}`),
}

export const templatesApi = {
  getAll: () => api.get<MailTemplate[]>('/api/templates'),
  getByCode: (code: string) => api.get<MailTemplate>(`/api/templates/${code}`),
  create: (data: CreateTemplate) => api.post('/api/templates', data),
  update: (code: string, data: CreateTemplate) => api.put(`/api/templates/${code}`, data),
  delete: (code: string) => api.delete(`/api/templates/${code}`),
}

export const logsApi = {
  getStatus: (requestId: string) => api.get<MailLogStatus>(`/api/mails/status/${requestId}`),
}

export const rateLimitsApi = {
  getAll: () => api.get<ClientApplication[]>('/api/clients'),
  create: (data: CreateClientApp) => api.post('/api/clients', data),
  update: (id: string, data: CreateClientApp) => api.put(`/api/clients/${id}`, data),
}

export default api
