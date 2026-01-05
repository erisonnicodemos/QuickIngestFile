import type { FilePreview } from '../api'

interface PreviewTableProps {
  preview: FilePreview
  maxRows?: number
}

export function PreviewTable({ preview, maxRows = 10 }: PreviewTableProps) {
  // Safety check for undefined preview or columns
  if (!preview || !preview.detectedColumns) {
    return null
  }

  const columns = preview.detectedColumns.filter((c) => !c.isIgnored)
  const rows = (preview.previewRows || []).slice(0, maxRows)

  const getTypeColor = (type: string) => {
    switch (type) {
      case 'integer':
        return 'bg-blue-100 text-blue-700'
      case 'decimal':
        return 'bg-purple-100 text-purple-700'
      case 'boolean':
        return 'bg-green-100 text-green-700'
      case 'datetime':
      case 'date':
        return 'bg-orange-100 text-orange-700'
      default:
        return 'bg-gray-100 text-gray-700'
    }
  }

  // Extrair formato do arquivo pelo nome
  const getFileFormat = (fileName: string): string => {
    const ext = fileName.split('.').pop()?.toLowerCase() || 'unknown'
    return ext
  }

  return (
    <div className="w-full">
      {/* Header Info */}
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-4">
          <span className="text-sm text-gray-500">
            <span className="font-medium text-gray-700">{preview.estimatedTotalRows}</span> rows detected
          </span>
          <span className="text-sm text-gray-500">
            <span className="font-medium text-gray-700">{columns.length}</span> columns
          </span>
          <span className="px-2 py-1 text-xs font-medium bg-primary-100 text-primary-700 rounded-full">
            {getFileFormat(preview.fileName).toUpperCase()}
          </span>
        </div>
      </div>

      {/* Table */}
      <div className="border border-gray-200 rounded-xl overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider border-b border-gray-200 w-12">
                  #
                </th>
                {columns.map((column) => (
                  <th
                    key={column.index}
                    className="px-4 py-3 text-left border-b border-gray-200"
                  >
                    <div className="flex flex-col gap-1">
                      <span className="text-sm font-medium text-gray-700 truncate max-w-[200px]">
                        {column.displayName || column.name}
                      </span>
                      <span
                        className={`inline-flex px-2 py-0.5 text-xs font-medium rounded-full w-fit ${getTypeColor(
                          column.detectedType
                        )}`}
                      >
                        {column.detectedType}
                      </span>
                    </div>
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-100">
              {rows.map((row, rowIndex) => (
                <tr key={rowIndex} className="hover:bg-gray-50 transition-colors">
                  <td className="px-4 py-3 text-sm text-gray-400 font-mono">
                    {rowIndex + 1}
                  </td>
                  {columns.map((column) => (
                    <td
                      key={column.index}
                      className="px-4 py-3 text-sm text-gray-600 truncate max-w-[300px]"
                      title={String(row[column.name] ?? '')}
                    >
                      {formatValue(row[column.name])}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {/* Show more indicator */}
        {preview.totalRows > maxRows && (
          <div className="px-4 py-3 bg-gray-50 border-t border-gray-200 text-center">
            <span className="text-sm text-gray-500">
              Showing {maxRows} of {preview.totalRows} rows
            </span>
          </div>
        )}
      </div>
    </div>
  )
}

function formatValue(value: unknown): string {
  if (value === null || value === undefined) return '—'
  if (typeof value === 'boolean') return value ? 'Yes' : 'No'
  if (typeof value === 'number') return value.toLocaleString()
  return String(value)
}

