import { useState, useCallback } from 'react'
import { FileUpload, PreviewTable, ImportProgressBar, DataTable, RecentJobs } from './components'
import { importApi, type FilePreview, type ImportProgress } from './api'

type AppState = 'upload' | 'preview' | 'importing' | 'viewing'

export default function App() {
  const [state, setState] = useState<AppState>('upload')
  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [preview, setPreview] = useState<FilePreview | null>(null)
  const [importProgress, setImportProgress] = useState<ImportProgress | null>(null)
  const [selectedJobId, setSelectedJobId] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [refreshTrigger, setRefreshTrigger] = useState(0)

  // Import options
  const [delimiter, setDelimiter] = useState<string>(',')
  const [hasHeader, setHasHeader] = useState<boolean>(true)

  const handleFileSelect = useCallback(async (file: File) => {
    setSelectedFile(file)
    setError(null)
    setIsLoading(true)

    try {
      const previewData = await importApi.preview(file, { delimiter, hasHeader })
      setPreview(previewData)
      setState('preview')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to preview file')
    } finally {
      setIsLoading(false)
    }
  }, [delimiter, hasHeader])

  const handleImport = useCallback(async () => {
    if (!selectedFile) return

    setIsLoading(true)
    setError(null)

    try {
      const progress = await importApi.import(selectedFile, { delimiter, hasHeader })
      setImportProgress(progress)
      setState('importing')

      // Poll for progress updates
      const pollProgress = async () => {
        if (progress.status === 'Completed' || progress.status === 'Failed') {
          setRefreshTrigger((t) => t + 1)
          if (progress.status === 'Completed') {
            setSelectedJobId(progress.importJobId)
            setState('viewing')
          }
          return
        }
        // In a real implementation, you'd poll the API here
        // For now, we assume it completes
        setTimeout(() => {
          setImportProgress((prev) =>
            prev ? { ...prev, status: 'Completed', progress: 100 } : null
          )
          setSelectedJobId(progress.importJobId)
          setRefreshTrigger((t) => t + 1)
          setState('viewing')
        }, 1500)
      }

      pollProgress()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to start import')
      setIsLoading(false)
    }
  }, [selectedFile, delimiter, hasHeader])

  const handleViewJob = useCallback((jobId: string) => {
    setSelectedJobId(jobId)
    setState('viewing')
  }, [])

  const handleNewImport = useCallback(() => {
    setState('upload')
    setSelectedFile(null)
    setPreview(null)
    setImportProgress(null)
    setSelectedJobId(null)
    setError(null)
  }, [])

  return (
    <div class="min-h-screen bg-gradient-to-br from-gray-50 to-gray-100">
      {/* Header */}
      <header class="bg-white border-b border-gray-200 sticky top-0 z-10">
        <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div class="flex items-center justify-between h-16">
            <div class="flex items-center gap-3">
              <div class="w-10 h-10 bg-gradient-to-br from-primary-500 to-primary-600 rounded-xl flex items-center justify-center">
                <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
                </svg>
              </div>
              <div>
                <h1 class="text-xl font-bold text-gray-800">QuickIngestFile</h1>
                <p class="text-xs text-gray-500">High-performance file import</p>
              </div>
            </div>

            {state !== 'upload' && (
              <button
                onClick={handleNewImport}
                class="px-4 py-2 text-sm font-medium text-primary-600 bg-primary-50 rounded-lg hover:bg-primary-100 transition-colors flex items-center gap-2"
              >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
                </svg>
                New Import
              </button>
            )}
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div class="grid grid-cols-1 lg:grid-cols-3 gap-8">
          {/* Main Area */}
          <div class="lg:col-span-2 space-y-6">
            {/* Error Alert */}
            {error && (
              <div class="p-4 bg-red-50 border border-red-200 rounded-xl flex items-start gap-3">
                <svg class="w-5 h-5 text-red-500 flex-shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <div class="flex-1">
                  <h4 class="text-sm font-medium text-red-800">Error</h4>
                  <p class="mt-1 text-sm text-red-600">{error}</p>
                </div>
                <button onClick={() => setError(null)} class="text-red-400 hover:text-red-600">
                  <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>
            )}

            {/* Upload State */}
            {state === 'upload' && (
              <div class="bg-white rounded-2xl shadow-sm border border-gray-200 p-6">
                <h2 class="text-lg font-semibold text-gray-800 mb-6">Upload File</h2>
                
                {/* Options */}
                <div class="mb-6 grid grid-cols-2 gap-4">
                  <div>
                    <label class="block text-sm font-medium text-gray-700 mb-2">
                      CSV Delimiter
                    </label>
                    <select
                      value={delimiter}
                      onChange={(e) => setDelimiter(e.target.value)}
                      class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 outline-none"
                    >
                      <option value=",">Comma (,)</option>
                      <option value=";">Semicolon (;)</option>
                      <option value="\t">Tab</option>
                      <option value="|">Pipe (|)</option>
                    </select>
                  </div>
                  <div>
                    <label class="block text-sm font-medium text-gray-700 mb-2">
                      First Row
                    </label>
                    <select
                      value={hasHeader ? 'header' : 'data'}
                      onChange={(e) => setHasHeader(e.target.value === 'header')}
                      class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 outline-none"
                    >
                      <option value="header">Column Headers</option>
                      <option value="data">Data Row</option>
                    </select>
                  </div>
                </div>

                <FileUpload onFileSelect={handleFileSelect} isLoading={isLoading} />
              </div>
            )}

            {/* Preview State */}
            {state === 'preview' && preview && (
              <div class="bg-white rounded-2xl shadow-sm border border-gray-200 p-6 space-y-6">
                <div class="flex items-center justify-between">
                  <div>
                    <h2 class="text-lg font-semibold text-gray-800">Preview</h2>
                    <p class="text-sm text-gray-500 mt-1">
                      Review the detected schema before importing
                    </p>
                  </div>
                  <div class="flex items-center gap-3">
                    <button
                      onClick={handleNewImport}
                      class="px-4 py-2 text-sm font-medium text-gray-600 bg-gray-100 rounded-lg hover:bg-gray-200 transition-colors"
                    >
                      Cancel
                    </button>
                    <button
                      onClick={handleImport}
                      disabled={isLoading}
                      class="px-6 py-2 text-sm font-medium text-white bg-primary-500 rounded-lg hover:bg-primary-600 disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex items-center gap-2"
                    >
                      {isLoading ? (
                        <>
                          <svg class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
                            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                          </svg>
                          Importing...
                        </>
                      ) : (
                        <>
                          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12" />
                          </svg>
                          Start Import
                        </>
                      )}
                    </button>
                  </div>
                </div>
                <PreviewTable preview={preview} />
              </div>
            )}

            {/* Importing State */}
            {state === 'importing' && importProgress && (
              <ImportProgressBar progress={importProgress} />
            )}

            {/* Viewing State */}
            {state === 'viewing' && selectedJobId && (
              <div class="bg-white rounded-2xl shadow-sm border border-gray-200 p-6">
                <h2 class="text-lg font-semibold text-gray-800 mb-6">Imported Data</h2>
                <DataTable importJobId={selectedJobId} />
              </div>
            )}
          </div>

          {/* Sidebar */}
          <div class="space-y-6">
            <RecentJobs onSelectJob={handleViewJob} refreshTrigger={refreshTrigger} />

            {/* Info Card */}
            <div class="bg-gradient-to-br from-primary-500 to-primary-600 rounded-2xl p-6 text-white">
              <h3 class="text-lg font-semibold mb-2">High Performance</h3>
              <p class="text-sm text-primary-100 mb-4">
                QuickIngestFile uses advanced streaming and batch processing to handle large files efficiently.
              </p>
              <div class="space-y-2 text-sm">
                <div class="flex items-center gap-2">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                  </svg>
                  <span>CSV & Excel support</span>
                </div>
                <div class="flex items-center gap-2">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                  </svg>
                  <span>Auto schema detection</span>
                </div>
                <div class="flex items-center gap-2">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                  </svg>
                  <span>Batch bulk inserts</span>
                </div>
                <div class="flex items-center gap-2">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                  </svg>
                  <span>SQL & MongoDB ready</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </main>

      {/* Footer */}
      <footer class="border-t border-gray-200 bg-white mt-12">
        <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
          <p class="text-center text-sm text-gray-500">
            QuickIngestFile - Built with .NET 8, React & Twind
          </p>
        </div>
      </footer>
    </div>
  )
}
