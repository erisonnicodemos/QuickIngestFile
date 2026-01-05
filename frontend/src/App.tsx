import { useState, useCallback, useRef, useEffect } from 'react'
import { FileUpload, PreviewTable, ImportProgressBar, DataTable, RecentJobs } from './components'
import { importApi, jobsApi, type FilePreview, type ImportProgress, type ImportJob } from './api'

type AppState = 'upload' | 'preview' | 'viewing'

interface ActiveImport {
  id: string
  fileName: string
  progress: ImportProgress
  startTime: number
  elapsedTime: number
}

export default function App() {
  const [state, setState] = useState<AppState>('upload')
  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [preview, setPreview] = useState<FilePreview | null>(null)
  const [selectedJobId, setSelectedJobId] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [refreshTrigger, setRefreshTrigger] = useState(0)
  
  // Multiple active imports
  const [activeImports, setActiveImports] = useState<ActiveImport[]>([])
  const pollingRefs = useRef<Map<string, NodeJS.Timeout>>(new Map())
  const timerRef = useRef<NodeJS.Timeout | null>(null)

  // Import options
  const [delimiter, setDelimiter] = useState<string>(',')
  const [hasHeader, setHasHeader] = useState<boolean>(true)

  // Timer effect for elapsed time of all active imports
  useEffect(() => {
    if (activeImports.length > 0) {
      timerRef.current = setInterval(() => {
        setActiveImports(prev => prev.map(imp => ({
          ...imp,
          elapsedTime: Date.now() - imp.startTime
        })))
      }, 100)
    } else {
      if (timerRef.current) {
        clearInterval(timerRef.current)
        timerRef.current = null
      }
    }
    return () => {
      if (timerRef.current) clearInterval(timerRef.current)
    }
  }, [activeImports.length])

  // Cleanup polling on unmount
  useEffect(() => {
    return () => {
      pollingRefs.current.forEach(timeout => clearTimeout(timeout))
      if (timerRef.current) clearInterval(timerRef.current)
    }
  }, [])

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

    const fileToImport = selectedFile
    const currentDelimiter = delimiter
    const currentHasHeader = hasHeader

    setIsLoading(true)
    setError(null)

    try {
      // Use async API - returns immediately with job ID
      const job: ImportJob = await importApi.importAsync(fileToImport, { 
        delimiter: currentDelimiter, 
        hasHeader: currentHasHeader 
      })
      
      // Add to active imports
      const newImport: ActiveImport = {
        id: job.id,
        fileName: fileToImport.name,
        startTime: Date.now(),
        elapsedTime: 0,
        progress: {
          importJobId: job.id,
          totalRecords: job.totalRecords,
          processedRecords: job.processedRecords,
          progress: 0,
          status: job.status || 'Pending'
        }
      }
      
      setActiveImports(prev => [...prev, newImport])
      
      // Reset form for new import
      setSelectedFile(null)
      setPreview(null)
      setState('upload')
      setIsLoading(false)

      // Poll for progress updates for this specific job
      const pollProgress = async () => {
        try {
          const updatedJob = await jobsApi.get(job.id)
          const progressPercent = updatedJob.totalRecords > 0 
            ? Math.round((updatedJob.processedRecords / updatedJob.totalRecords) * 100) 
            : 0

          setActiveImports(prev => prev.map(imp => 
            imp.id === job.id 
              ? {
                  ...imp,
                  progress: {
                    importJobId: updatedJob.id,
                    totalRecords: updatedJob.totalRecords,
                    processedRecords: updatedJob.processedRecords,
                    progress: progressPercent,
                    status: updatedJob.status,
                    errorMessage: updatedJob.errorMessage,
                    startedAt: updatedJob.startedAt,
                    completedAt: updatedJob.completedAt,
                    durationMs: updatedJob.durationMs
                  }
                }
              : imp
          ))

          if (updatedJob.status === 'Completed' || updatedJob.status === 'CompletedWithErrors' || updatedJob.status === 'Failed') {
            // Remove from active imports after a delay to show final state
            setTimeout(() => {
              setActiveImports(prev => prev.filter(imp => imp.id !== job.id))
              pollingRefs.current.delete(job.id)
            }, 3000)
            setRefreshTrigger((t) => t + 1)
            return
          }

          // Continue polling
          const timeout = setTimeout(pollProgress, 500)
          pollingRefs.current.set(job.id, timeout)
        } catch (err) {
          console.error('Error polling progress:', err)
          const timeout = setTimeout(pollProgress, 1000)
          pollingRefs.current.set(job.id, timeout)
        }
      }

      // Start polling immediately for async jobs
      const timeout = setTimeout(pollProgress, 200)
      pollingRefs.current.set(job.id, timeout)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to start import')
      setIsLoading(false)
    }
  }, [selectedFile, delimiter, hasHeader])

  const handleViewJob = useCallback((jobId: string) => {
    setSelectedJobId(jobId)
    setState('viewing')
  }, [])

  const handleBackToUpload = useCallback(() => {
    setState('upload')
    setSelectedFile(null)
    setPreview(null)
    setSelectedJobId(null)
    setError(null)
  }, [])

  const handleCancelPreview = useCallback(() => {
    setState('upload')
    setSelectedFile(null)
    setPreview(null)
    setError(null)
  }, [])

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-50 to-gray-100">
      {/* Header */}
      <header className="bg-white border-b border-gray-200 sticky top-0 z-10">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between h-16">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 bg-gradient-to-br from-primary-500 to-primary-600 rounded-xl flex items-center justify-center">
                <svg className="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
                </svg>
              </div>
              <div>
                <h1 className="text-xl font-bold text-gray-800">QuickIngestFile</h1>
                <p className="text-xs text-gray-500">High-performance file import</p>
              </div>
            </div>

            {state === 'viewing' && (
              <button
                onClick={handleBackToUpload}
                className="px-4 py-2 text-sm font-medium text-primary-600 bg-primary-50 rounded-lg hover:bg-primary-100 transition-colors flex items-center gap-2"
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 4v16m8-8H4" />
                </svg>
                New Import
              </button>
            )}
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          {/* Main Area */}
          <div className="lg:col-span-2 space-y-6">
            {/* Error Alert */}
            {error && (
              <div className="p-4 bg-red-50 border border-red-200 rounded-xl flex items-start gap-3">
                <svg className="w-5 h-5 text-red-500 flex-shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <div className="flex-1">
                  <h4 className="text-sm font-medium text-red-800">Error</h4>
                  <p className="mt-1 text-sm text-red-600">{error}</p>
                </div>
                <button onClick={() => setError(null)} className="text-red-400 hover:text-red-600">
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>
            )}

            {/* Active Imports Panel */}
            {activeImports.length > 0 && (
              <div className="bg-white rounded-2xl shadow-sm border border-gray-200 p-6">
                <div className="flex items-center justify-between mb-4">
                  <h2 className="text-lg font-semibold text-gray-800 flex items-center gap-2">
                    <svg className="w-5 h-5 text-primary-500 animate-pulse" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                    </svg>
                    Active Imports ({activeImports.length})
                  </h2>
                </div>
                <div className="space-y-4">
                  {activeImports.map((imp) => (
                    <div key={imp.id} className="bg-gray-50 rounded-xl border border-gray-200 p-4">
                      <div className="flex items-center justify-between mb-2">
                        <div className="flex items-center gap-2">
                          <svg className="w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                          </svg>
                          <span className="text-sm font-medium text-gray-700 truncate max-w-xs">{imp.fileName}</span>
                        </div>
                        <span className={`text-xs px-2 py-1 rounded-full ${
                          imp.progress.status === 'Completed' || imp.progress.status === 'CompletedWithErrors'
                            ? 'bg-green-100 text-green-700'
                            : imp.progress.status === 'Failed'
                            ? 'bg-red-100 text-red-700'
                            : 'bg-blue-100 text-blue-700'
                        }`}>
                          {imp.progress.status}
                        </span>
                      </div>
                      <ImportProgressBar progress={imp.progress} />
                      <div className="mt-2 flex items-center justify-between text-xs text-gray-500">
                        <span>
                          {(imp.progress.processedRecords || 0).toLocaleString()} / {(imp.progress.totalRecords || 0).toLocaleString() || '?'} records
                        </span>
                        <span className="font-mono">
                          {(imp.elapsedTime / 1000).toFixed(1)}s
                        </span>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* Upload State */}
            {state === 'upload' && (
              <div className="bg-white rounded-2xl shadow-sm border border-gray-200 p-6">
                <h2 className="text-lg font-semibold text-gray-800 mb-6">Upload File</h2>
                
                {/* Options */}
                <div className="mb-6 grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      CSV Delimiter
                    </label>
                    <select
                      value={delimiter}
                      onChange={(e) => setDelimiter(e.target.value)}
                      className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 outline-none"
                    >
                      <option value=",">Comma (,)</option>
                      <option value=";">Semicolon (;)</option>
                      <option value="\t">Tab</option>
                      <option value="|">Pipe (|)</option>
                    </select>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      First Row
                    </label>
                    <select
                      value={hasHeader ? 'header' : 'data'}
                      onChange={(e) => setHasHeader(e.target.value === 'header')}
                      className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 outline-none"
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
              <div className="bg-white rounded-2xl shadow-sm border border-gray-200 p-6 space-y-6">
                <div className="flex items-center justify-between">
                  <div>
                    <h2 className="text-lg font-semibold text-gray-800">Preview</h2>
                    <p className="text-sm text-gray-500 mt-1">
                      Review the detected schema before importing
                    </p>
                  </div>
                  <div className="flex items-center gap-3">
                    <button
                      onClick={handleCancelPreview}
                      className="px-4 py-2 text-sm font-medium text-gray-600 bg-gray-100 rounded-lg hover:bg-gray-200 transition-colors"
                    >
                      Cancel
                    </button>
                    <button
                      onClick={handleImport}
                      disabled={isLoading}
                      className="px-6 py-2 text-sm font-medium text-white bg-primary-500 rounded-lg hover:bg-primary-600 disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex items-center gap-2"
                    >
                      {isLoading ? (
                        <>
                          <svg className="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
                            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                          </svg>
                          Starting...
                        </>
                      ) : (
                        <>
                          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12" />
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

            {/* Viewing State */}
            {state === 'viewing' && selectedJobId && (
              <div className="bg-white rounded-2xl shadow-sm border border-gray-200 p-6">
                <h2 className="text-lg font-semibold text-gray-800 mb-6">Imported Data</h2>
                <DataTable importJobId={selectedJobId} />
              </div>
            )}
          </div>

          {/* Sidebar */}
          <div className="space-y-6">
            <RecentJobs onSelectJob={handleViewJob} refreshTrigger={refreshTrigger} />

            {/* Info Card */}
            <div className="bg-gradient-to-br from-primary-500 to-primary-600 rounded-2xl p-6 text-white">
              <h3 className="text-lg font-semibold mb-2">High Performance</h3>
              <p className="text-sm text-primary-100 mb-4">
                QuickIngestFile uses advanced streaming and batch processing to handle large files efficiently.
              </p>
              <div className="space-y-2 text-sm">
                <div className="flex items-center gap-2">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M5 13l4 4L19 7" />
                  </svg>
                  <span>CSV & Excel support</span>
                </div>
                <div className="flex items-center gap-2">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M5 13l4 4L19 7" />
                  </svg>
                  <span>Auto schema detection</span>
                </div>
                <div className="flex items-center gap-2">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M5 13l4 4L19 7" />
                  </svg>
                  <span>Batch bulk inserts</span>
                </div>
                <div className="flex items-center gap-2">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M5 13l4 4L19 7" />
                  </svg>
                  <span>SQL & MongoDB ready</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </main>

      {/* Footer */}
      <footer className="border-t border-gray-200 bg-white mt-12">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
          <p className="text-center text-sm text-gray-500">
            QuickIngestFile - Built with .NET 8, React & Twind
          </p>
        </div>
      </footer>
    </div>
  )
}

