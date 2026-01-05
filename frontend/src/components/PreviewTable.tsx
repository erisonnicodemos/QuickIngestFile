import type { FilePreview } from '../api'

interface PreviewTableProps {
  preview: FilePreview
  maxRows?: number
}

export function PreviewTable({ preview, maxRows = 10 }: PreviewTableProps) {
  const columns = preview.schema.columns.filter((c) => !c.isIgnored)
  const rows = preview.previewRows.slice(0, maxRows)

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

  return (
    <div class="w-full">
      {/* Header Info */}
      <div class="flex items-center justify-between mb-4">
        <div class="flex items-center gap-4">
          <span class="text-sm text-gray-500">
            <span class="font-medium text-gray-700">{preview.totalRows}</span> rows detected
          </span>
          <span class="text-sm text-gray-500">
            <span class="font-medium text-gray-700">{columns.length}</span> columns
          </span>
          <span class="px-2 py-1 text-xs font-medium bg-primary-100 text-primary-700 rounded-full">
            {preview.detectedFormat.toUpperCase()}
          </span>
        </div>
      </div>

      {/* Table */}
      <div class="border border-gray-200 rounded-xl overflow-hidden">
        <div class="overflow-x-auto">
          <table class="w-full">
            <thead class="bg-gray-50">
              <tr>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider border-b border-gray-200 w-12">
                  #
                </th>
                {columns.map((column) => (
                  <th
                    key={column.index}
                    class="px-4 py-3 text-left border-b border-gray-200"
                  >
                    <div class="flex flex-col gap-1">
                      <span class="text-sm font-medium text-gray-700 truncate max-w-[200px]">
                        {column.displayName || column.name}
                      </span>
                      <span
                        class={`inline-flex px-2 py-0.5 text-xs font-medium rounded-full w-fit ${getTypeColor(
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
            <tbody class="bg-white divide-y divide-gray-100">
              {rows.map((row, rowIndex) => (
                <tr key={rowIndex} class="hover:bg-gray-50 transition-colors">
                  <td class="px-4 py-3 text-sm text-gray-400 font-mono">
                    {rowIndex + 1}
                  </td>
                  {columns.map((column) => (
                    <td
                      key={column.index}
                      class="px-4 py-3 text-sm text-gray-600 truncate max-w-[300px]"
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
          <div class="px-4 py-3 bg-gray-50 border-t border-gray-200 text-center">
            <span class="text-sm text-gray-500">
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
