import axios from 'axios'

const API_URL = import.meta.env.VITE_API_URL || ''

const api = axios.create({
  baseURL: `${API_URL}/api`,
  headers: {
    'Content-Type': 'application/json',
  },
})

// Types
export interface FilePreview {
  fileName: string
  fileSize: number
  detectedColumns: ColumnDefinition[]
  previewRows: Record<string, unknown>[]
  estimatedTotalRows: number
}

export interface FileSchema {
  id: string
  importJobId: string
  fileName: string
  columns: ColumnDefinition[]
}

export interface ColumnDefinition {
  name: string
  index: number
  detectedType: string
  displayName?: string
  isIgnored: boolean
}

export interface ImportJob {
  id: string
  fileName: string
  fileType: string
  fileSize: number
  status: string
  totalRecords: number
  processedRecords: number
  failedRecords: number
  errorMessage?: string
  startedAt?: string
  completedAt?: string
  createdAt: string
  durationMs?: number
}

export interface ImportProgress {
  importJobId: string
  totalRecords: number
  processedRecords: number
  progress: number
  status: string
  errorMessage?: string
  startedAt?: string
  completedAt?: string
  durationMs?: number
}

export interface ImportResult {
  importJobId: string
  recordsImported: number
  duration: string
}

export interface ImportedRecord {
  id: string
  importJobId: string
  data: Record<string, unknown>
  rowNumber: number
  createdAt: string
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

// API Functions
export const importApi = {
  // Get file preview
  preview: async (file: File, options?: { delimiter?: string; hasHeader?: boolean }): Promise<FilePreview> => {
    const formData = new FormData()
    formData.append('file', file)
    if (options?.delimiter) formData.append('delimiter', options.delimiter)
    if (options?.hasHeader !== undefined) formData.append('hasHeader', String(options.hasHeader))

    const response = await api.post<FilePreview>('/import/preview', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    return response.data
  },

  // Start import (synchronous - waits for completion)
  import: async (file: File, options?: { delimiter?: string; hasHeader?: boolean }): Promise<ImportProgress> => {
    const formData = new FormData()
    formData.append('file', file)
    if (options?.delimiter) formData.append('delimiter', options.delimiter)
    if (options?.hasHeader !== undefined) formData.append('hasHeader', String(options.hasHeader))

    const response = await api.post<ImportProgress>('/import', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    return response.data
  },

  // Start import (asynchronous - returns immediately, processes in background)
  // Allows parallel imports of multiple files
  importAsync: async (file: File, options?: { delimiter?: string; hasHeader?: boolean }): Promise<ImportJob> => {
    const formData = new FormData()
    formData.append('file', file)
    
    const params = new URLSearchParams()
    if (options?.delimiter) params.append('delimiter', options.delimiter)
    if (options?.hasHeader !== undefined) params.append('hasHeader', String(options.hasHeader))

    const response = await api.post<ImportJob>(`/import/async?${params.toString()}`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    return response.data
  },

  // Get supported formats
  formats: async (): Promise<string[]> => {
    const response = await api.get<string[]>('/import/formats')
    return response.data
  },
}

export const dataApi = {
  // Get schema for import job
  getSchema: async (importJobId: string): Promise<FileSchema> => {
    const response = await api.get<FileSchema>(`/data/${importJobId}/schema`)
    return response.data
  },

  // Get records for import job (paged)
  getRecords: async (
    importJobId: string,
    page: number = 1,
    pageSize: number = 50
  ): Promise<PagedResult<ImportedRecord>> => {
    const response = await api.get<PagedResult<ImportedRecord>>(
      `/data/${importJobId}/records?page=${page}&pageSize=${pageSize}`
    )
    return response.data
  },

  // Search records
  search: async (
    importJobId: string,
    searchTerm: string,
    page: number = 1,
    pageSize: number = 50
  ): Promise<PagedResult<ImportedRecord>> => {
    const response = await api.get<PagedResult<ImportedRecord>>(
      `/data/${importJobId}/search?searchTerm=${encodeURIComponent(searchTerm)}&page=${page}&pageSize=${pageSize}`
    )
    return response.data
  },

  // Export records
  export: async (importJobId: string, format: 'csv' | 'json' = 'csv'): Promise<Blob> => {
    const response = await api.get(`/data/${importJobId}/export?format=${format}`, {
      responseType: 'blob',
    })
    return response.data
  },
}

export const jobsApi = {
  // Get recent jobs
  getRecent: async (count: number = 10): Promise<ImportJob[]> => {
    const response = await api.get<ImportJob[]>(`/jobs/recent?count=${count}`)
    return response.data
  },

  // Get job by ID
  get: async (id: string): Promise<ImportJob> => {
    const response = await api.get<ImportJob>(`/jobs/${id}`)
    return response.data
  },

  // Delete job
  delete: async (id: string): Promise<void> => {
    await api.delete(`/jobs/${id}`)
  },
}

export default api
