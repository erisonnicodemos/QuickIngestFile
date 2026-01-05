import type { ImportProgress } from '../api'

interface ImportProgressBarProps {
  progress: ImportProgress
}

export function ImportProgressBar({ progress }: ImportProgressBarProps) {
  const getStatusColor = () => {
    switch (progress.status) {
      case 'Completed':
        return 'bg-accent-500'
      case 'Failed':
        return 'bg-red-500'
      case 'Processing':
        return 'bg-primary-500'
      default:
        return 'bg-gray-400'
    }
  }

  const getStatusBgColor = () => {
    switch (progress.status) {
      case 'Completed':
        return 'bg-accent-100'
      case 'Failed':
        return 'bg-red-100'
      case 'Processing':
        return 'bg-primary-100'
      default:
        return 'bg-gray-100'
    }
  }

  const getStatusIcon = () => {
    switch (progress.status) {
      case 'Completed':
        return (
          <svg class="w-5 h-5 text-accent-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
          </svg>
        )
      case 'Failed':
        return (
          <svg class="w-5 h-5 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
        )
      case 'Processing':
        return (
          <svg class="w-5 h-5 text-primary-600 animate-spin" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
          </svg>
        )
      default:
        return (
          <svg class="w-5 h-5 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
        )
    }
  }

  return (
    <div class="w-full p-6 bg-white rounded-xl border border-gray-200 shadow-sm">
      <div class="flex items-center justify-between mb-4">
        <div class="flex items-center gap-3">
          <div class={`p-2 rounded-lg ${getStatusBgColor()}`}>
            {getStatusIcon()}
          </div>
          <div>
            <h3 class="text-lg font-semibold text-gray-800">
              {progress.status === 'Completed' ? 'Import Complete!' : 
               progress.status === 'Failed' ? 'Import Failed' :
               progress.status === 'Processing' ? 'Importing...' : 'Pending'}
            </h3>
            <p class="text-sm text-gray-500">
              {progress.processedRecords.toLocaleString()} of {progress.totalRecords.toLocaleString()} records
            </p>
          </div>
        </div>
        <div class="text-right">
          <span class="text-2xl font-bold text-gray-800">{progress.progress}%</span>
        </div>
      </div>

      {/* Progress Bar */}
      <div class="relative">
        <div class="h-3 bg-gray-100 rounded-full overflow-hidden">
          <div
            class={`h-full rounded-full transition-all duration-500 ease-out ${getStatusColor()}`}
            style={{ width: `${progress.progress}%` }}
          />
        </div>
        
        {/* Animated shine effect for processing */}
        {progress.status === 'Processing' && (
          <div
            class="absolute top-0 left-0 h-full w-full overflow-hidden rounded-full"
            style={{ width: `${progress.progress}%` }}
          >
            <div class="absolute inset-0 bg-gradient-to-r from-transparent via-white/30 to-transparent animate-[shimmer_2s_infinite]" />
          </div>
        )}
      </div>

      {/* Stats */}
      <div class="mt-4 flex items-center justify-between text-sm">
        <span class="text-gray-500">
          Job ID: <code class="text-gray-700 bg-gray-100 px-2 py-0.5 rounded">{progress.importJobId.slice(0, 8)}...</code>
        </span>
        {progress.status === 'Processing' && (
          <span class="text-primary-600 flex items-center gap-2">
            <span class="w-2 h-2 bg-primary-500 rounded-full animate-pulse" />
            Processing...
          </span>
        )}
      </div>
    </div>
  )
}
