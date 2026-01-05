import { useCallback, useState } from 'react'
import { useDropzone } from 'react-dropzone'

interface FileUploadProps {
  onFileSelect: (file: File) => void
  acceptedFormats?: string[]
  maxSizeMB?: number
  isLoading?: boolean
}

export function FileUpload({
  onFileSelect,
  acceptedFormats = ['.csv', '.xlsx', '.xls'],
  maxSizeMB = 100,
  isLoading = false,
}: FileUploadProps) {
  const [error, setError] = useState<string | null>(null)

  const onDrop = useCallback(
    (acceptedFiles: File[], rejectedFiles: unknown[]) => {
      setError(null)

      if (rejectedFiles.length > 0) {
        setError('Invalid file. Please upload a CSV or Excel file.')
        return
      }

      if (acceptedFiles.length > 0) {
        const file = acceptedFiles[0]
        if (file.size > maxSizeMB * 1024 * 1024) {
          setError(`File size exceeds ${maxSizeMB}MB limit.`)
          return
        }
        onFileSelect(file)
      }
    },
    [onFileSelect, maxSizeMB]
  )

  const { getRootProps, getInputProps, isDragActive, isDragReject } = useDropzone({
    onDrop,
    accept: {
      'text/csv': ['.csv'],
      'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet': ['.xlsx'],
      'application/vnd.ms-excel': ['.xls'],
    },
    maxFiles: 1,
    disabled: isLoading,
  })

  return (
    <div className="w-full">
      <div
        {...getRootProps()}
        className={`
          relative border-2 border-dashed rounded-xl p-12 text-center cursor-pointer
          transition-all duration-300 ease-in-out
          ${isDragActive && !isDragReject ? 'border-primary-500 bg-primary-50 scale-[1.02]' : ''}
          ${isDragReject ? 'border-red-500 bg-red-50' : ''}
          ${!isDragActive && !isDragReject ? 'border-gray-300 hover:border-primary-400 hover:bg-gray-50' : ''}
          ${isLoading ? 'opacity-50 cursor-not-allowed' : ''}
        `}
      >
        <input {...getInputProps()} />

        <div className="flex flex-col items-center gap-4">
          {/* Upload Icon */}
          <div
            className={`
              w-16 h-16 rounded-full flex items-center justify-center
              transition-colors duration-300
              ${isDragActive ? 'bg-primary-100' : 'bg-gray-100'}
            `}
          >
            {isLoading ? (
              <svg
                className="w-8 h-8 text-primary-500 animate-spin"
                fill="none"
                viewBox="0 0 24 24"
              >
                <circle
                  className="opacity-25"
                  cx="12"
                  cy="12"
                  r="10"
                  stroke="currentColor"
                  strokeWidth="4"
                />
                <path
                  className="opacity-75"
                  fill="currentColor"
                  d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
                />
              </svg>
            ) : (
              <svg
                className={`w-8 h-8 transition-colors duration-300 ${
                  isDragActive ? 'text-primary-500' : 'text-gray-400'
                }`}
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth="2"
                  d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"
                />
              </svg>
            )}
          </div>

          {/* Text */}
          <div>
            {isLoading ? (
              <p className="text-lg font-medium text-gray-600">Processing file...</p>
            ) : isDragActive ? (
              <p className="text-lg font-medium text-primary-600">Drop your file here!</p>
            ) : (
              <>
                <p className="text-lg font-medium text-gray-700">
                  Drag & drop your file here
                </p>
                <p className="text-sm text-gray-500 mt-1">
                  or <span className="text-primary-500 underline">click to browse</span>
                </p>
              </>
            )}
          </div>

          {/* Supported formats */}
          <div className="flex flex-wrap gap-2 justify-center">
            {acceptedFormats.map((format) => (
              <span
                key={format}
                className="px-3 py-1 text-xs font-medium bg-gray-100 text-gray-600 rounded-full"
              >
                {format.toUpperCase()}
              </span>
            ))}
          </div>

          <p className="text-xs text-gray-400">Maximum file size: {maxSizeMB}MB</p>
        </div>
      </div>

      {/* Error Message */}
      {error && (
        <div className="mt-4 p-4 bg-red-50 border border-red-200 rounded-lg flex items-center gap-3">
          <svg
            className="w-5 h-5 text-red-500 flex-shrink-0"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth="2"
              d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
            />
          </svg>
          <p className="text-sm text-red-600">{error}</p>
        </div>
      )}
    </div>
  )
}

