import { useState, useEffect } from 'react'
import type { ImportJob } from '../api'
import { jobsApi } from '../api'

interface RecentJobsProps {
  onSelectJob: (jobId: string) => void
  refreshTrigger?: number
}

export function RecentJobs({ onSelectJob, refreshTrigger }: RecentJobsProps) {
  const [jobs, setJobs] = useState<ImportJob[]>([])
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    const loadJobs = async () => {
      setIsLoading(true)
      try {
        const data = await jobsApi.getRecent(10)
        setJobs(data)
      } catch {
        // Silently fail
      } finally {
        setIsLoading(false)
      }
    }
    loadJobs()
  }, [refreshTrigger])

  const handleDelete = async (id: string, e: React.MouseEvent) => {
    e.stopPropagation()
    if (!confirm('Are you sure you want to delete this import job?')) return
    
    try {
      await jobsApi.delete(id)
      setJobs((prev) => prev.filter((j) => j.id !== id))
    } catch {
      // Silently fail
    }
  }

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'Completed':
        return 'bg-accent-100 text-accent-700'
      case 'Failed':
        return 'bg-red-100 text-red-700'
      case 'Processing':
        return 'bg-primary-100 text-primary-700'
      default:
        return 'bg-gray-100 text-gray-700'
    }
  }

  const formatDate = (date: string) => {
    return new Date(date).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    })
  }

  if (isLoading) {
    return (
      <div className="p-6 bg-white rounded-xl border border-gray-200">
        <div className="animate-pulse space-y-4">
          <div className="h-4 bg-gray-200 rounded w-1/4" />
          <div className="space-y-3">
            {[1, 2, 3].map((i) => (
              <div key={i} className="h-16 bg-gray-100 rounded" />
            ))}
          </div>
        </div>
      </div>
    )
  }

  if (jobs.length === 0) {
    return (
      <div className="p-8 bg-white rounded-xl border border-gray-200 text-center">
        <svg
          className="w-12 h-12 mx-auto text-gray-300"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth="2"
            d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
          />
        </svg>
        <h3 className="mt-4 text-lg font-medium text-gray-700">No imports yet</h3>
        <p className="mt-2 text-sm text-gray-500">
          Upload a file to start importing data
        </p>
      </div>
    )
  }

  return (
    <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
      <div className="px-6 py-4 border-b border-gray-200">
        <h3 className="text-lg font-semibold text-gray-800">Recent Imports</h3>
      </div>
      <div className="divide-y divide-gray-100">
        {jobs.map((job) => (
          <div
            key={job.id}
            onClick={() => onSelectJob(job.id)}
            className="px-6 py-4 hover:bg-gray-50 cursor-pointer transition-colors flex items-center justify-between group"
          >
            <div className="flex-1 min-w-0">
              <div className="flex items-center gap-3">
                <h4 className="text-sm font-medium text-gray-800 truncate">
                  {job.fileName}
                </h4>
                <span
                  className={`px-2 py-0.5 text-xs font-medium rounded-full ${getStatusBadge(
                    job.status
                  )}`}
                >
                  {job.status}
                </span>
              </div>
              <div className="mt-1 flex items-center gap-4 text-xs text-gray-500">
                <span>{job.totalRecords.toLocaleString()} records</span>
                <span>{formatDate(job.createdAt)}</span>
              </div>
            </div>
            <button
              onClick={(e) => handleDelete(job.id, e)}
              className="ml-4 p-2 text-gray-400 hover:text-red-500 opacity-0 group-hover:opacity-100 transition-all"
              title="Delete"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth="2"
                  d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
                />
              </svg>
            </button>
          </div>
        ))}
      </div>
    </div>
  )
}

