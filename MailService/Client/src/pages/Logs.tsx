import { useState } from 'react'
import api from '../services/api'

export default function Logs() {
  const [searchMode, setSearchMode] = useState<'requestId' | 'filter'>('requestId')
  const [requestId, setRequestId] = useState('')
  const [clientIdName, setClientIdName] = useState('')
  const [startDate, setStartDate] = useState('')
  const [endDate, setEndDate] = useState('')
  const [result, setResult] = useState<any>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Success':
        return 'bg-green-100 text-green-700'
      case 'Failed':
        return 'bg-red-100 text-red-700'
      case 'Queued':
        return 'bg-yellow-100 text-yellow-700'
      default:
        return 'bg-gray-100 text-gray-700'
    }
  }

  const handleSearchByRequestId = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!requestId.trim()) return

    setLoading(true)
    setError('')
    setResult(null)

    try {
      const res = await api.get(`/api/mails/status/${requestId}`)
      setResult({ type: 'requestId', data: res.data })
    } catch (err: any) {
      if (err.response?.status === 404) {
        setError('Request ID bulunamadı')
      } else {
        setError('Bir hata oluştu: ' + (err.response?.data?.title || err.message))
      }
    } finally {
      setLoading(false)
    }
  }

  const handleSearchByFilter = async (e: React.FormEvent) => {
    e.preventDefault()

    // Her ikisi de zorunlu
    if (!clientIdName.trim() || !startDate || !endDate) {
      setError('Client ID, Başlangıç ve Bitiş Tarihi girilmeli')
      return
    }

    setLoading(true)
    setError('')
    setResult(null)

    try {
      const params = new URLSearchParams()
      params.append('clientIdName', clientIdName)
      params.append('startDate', new Date(startDate).toISOString())
      params.append('endDate', new Date(endDate + 'T23:59:59').toISOString())

      const res = await api.get(`/api/mails/search?${params.toString()}`)
      setResult({ type: 'filter', data: res.data })
    } catch (err: any) {
      setError(err.response?.data?.Error || 'Bir hata oluştu')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div>
      <h1 className="text-2xl font-bold text-gray-800 mb-6">Mail Logları</h1>

      {/* Search Mode Tabs */}
      <div className="bg-white rounded-xl shadow-sm border mb-6">
        <div className="flex border-b">
          <button
            onClick={() => { setSearchMode('requestId'); setResult(null); setError('') }}
            className={`px-6 py-3 text-sm font-medium transition-colors ${
              searchMode === 'requestId'
                ? 'text-blue-600 border-b-2 border-blue-600'
                : 'text-gray-500 hover:text-gray-700'
            }`}
          >
            Request ID ile Ara
          </button>
          <button
            onClick={() => { setSearchMode('filter'); setResult(null); setError('') }}
            className={`px-6 py-3 text-sm font-medium transition-colors ${
              searchMode === 'filter'
                ? 'text-blue-600 border-b-2 border-blue-600'
                : 'text-gray-500 hover:text-gray-700'
            }`}
          >
            Tarih ve Client ile Ara
          </button>
        </div>

        {/* Request ID Search */}
        {searchMode === 'requestId' && (
          <form onSubmit={handleSearchByRequestId} className="p-6">
            <div className="flex gap-3">
              <input
                type="text"
                value={requestId}
                onChange={(e) => setRequestId(e.target.value)}
                placeholder="Request ID (UUID) girin"
                className="flex-1 px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none font-mono text-sm"
              />
              <button
                type="submit"
                disabled={loading}
                className="bg-blue-600 text-white px-6 py-2 rounded-lg text-sm font-medium hover:bg-blue-700 disabled:opacity-50"
              >
                {loading ? 'Sorgulanıyor...' : 'Sorgula'}
              </button>
            </div>
          </form>
        )}

        {/* Filter Search */}
        {searchMode === 'filter' && (
          <form onSubmit={handleSearchByFilter} className="p-6">
            <div className="bg-blue-50 border border-blue-200 rounded-lg p-3 mb-4">
              <p className="text-sm text-blue-700">
                <strong>Not:</strong> Client ID ve Tarih aralığı <strong>her ikisi de</strong> girilmeli.
              </p>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Client ID</label>
                <input
                  type="text"
                  value={clientIdName}
                  onChange={(e) => setClientIdName(e.target.value)}
                  placeholder="erp-api, crm-api..."
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none text-sm"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Başlangıç Tarihi</label>
                <input
                  type="date"
                  value={startDate}
                  onChange={(e) => setStartDate(e.target.value)}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none text-sm"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Bitiş Tarihi</label>
                <input
                  type="date"
                  value={endDate}
                  onChange={(e) => setEndDate(e.target.value)}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none text-sm"
                />
              </div>
              <div className="flex items-end">
                <button
                  type="submit"
                  disabled={loading}
                  className="w-full bg-blue-600 text-white px-6 py-2 rounded-lg text-sm font-medium hover:bg-blue-700 disabled:opacity-50"
                >
                  {loading ? 'Sorgulanıyor...' : 'Sorgula'}
                </button>
              </div>
            </div>
          </form>
        )}
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-600 px-4 py-3 rounded-lg mb-6 text-sm">
          {error}
        </div>
      )}

      {/* Results - Request ID Mode */}
      {result && result.type === 'requestId' && (
        <div className="space-y-6">
          <div className="bg-white p-6 rounded-xl shadow-sm border">
            <h2 className="text-lg font-semibold text-gray-800 mb-4">Özet</h2>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              <div className="bg-gray-50 p-4 rounded-lg">
                <p className="text-xs text-gray-500">Toplam</p>
                <p className="text-2xl font-bold text-gray-800">{result.data.total}</p>
              </div>
              <div className="bg-green-50 p-4 rounded-lg">
                <p className="text-xs text-gray-500">Başarılı</p>
                <p className="text-2xl font-bold text-green-600">{result.data.successCount}</p>
              </div>
              <div className="bg-red-50 p-4 rounded-lg">
                <p className="text-xs text-gray-500">Başarısız</p>
                <p className="text-2xl font-bold text-red-600">{result.data.failedCount}</p>
              </div>
              <div className="bg-yellow-50 p-4 rounded-lg">
                <p className="text-xs text-gray-500">Beklemede</p>
                <p className="text-2xl font-bold text-yellow-600">{result.data.queuedCount}</p>
              </div>
            </div>
          </div>

          <div className="bg-white rounded-xl shadow-sm border overflow-hidden">
            <div className="p-6 border-b">
              <h2 className="text-lg font-semibold text-gray-800">Alıcı Detayları</h2>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Alıcı</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Konu</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Durum</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Tarih</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-200">
                  {result.data.recipients?.map((r: any, i: number) => (
                    <tr key={i} className="hover:bg-gray-50">
                      <td className="px-6 py-4 text-sm font-mono text-gray-800">{r.to}</td>
                      <td className="px-6 py-4 text-sm text-gray-600">{r.subject}</td>
                      <td className="px-6 py-4">
                        <span className={`px-2 py-1 text-xs rounded ${getStatusColor(r.status)}`}>
                          {r.status}
                        </span>
                      </td>
                      <td className="px-6 py-4 text-sm text-gray-500">
                        {new Date(r.sentDate).toLocaleString('tr-TR')}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {/* Results - Filter Mode */}
      {result && result.type === 'filter' && (
        <div className="space-y-6">
          <div className="bg-white p-6 rounded-xl shadow-sm border">
            <h2 className="text-lg font-semibold text-gray-800 mb-4">Özet</h2>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              <div className="bg-gray-50 p-4 rounded-lg">
                <p className="text-xs text-gray-500">Toplam</p>
                <p className="text-2xl font-bold text-gray-800">{result.data.total}</p>
              </div>
              <div className="bg-green-50 p-4 rounded-lg">
                <p className="text-xs text-gray-500">Başarılı</p>
                <p className="text-2xl font-bold text-green-600">{result.data.successCount}</p>
              </div>
              <div className="bg-red-50 p-4 rounded-lg">
                <p className="text-xs text-gray-500">Başarısız</p>
                <p className="text-2xl font-bold text-red-600">{result.data.failedCount}</p>
              </div>
              <div className="bg-yellow-50 p-4 rounded-lg">
                <p className="text-xs text-gray-500">Beklemede</p>
                <p className="text-2xl font-bold text-yellow-600">{result.data.queuedCount}</p>
              </div>
            </div>
          </div>

          <div className="bg-white rounded-xl shadow-sm border overflow-hidden">
            <div className="p-6 border-b flex justify-between items-center">
              <h2 className="text-lg font-semibold text-gray-800">Loglar (Son 500 kayıt)</h2>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Request ID</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Client</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Alıcı</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Konu</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Durum</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Tarih</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-200">
                  {result.data.logs?.map((l: any, i: number) => (
                    <tr key={i} className="hover:bg-gray-50">
                      <td className="px-6 py-4 text-sm font-mono text-gray-800 truncate max-w-32">
                        {l.requestId}
                      </td>
                      <td className="px-6 py-4 text-sm text-gray-600">{l.clientIdName}</td>
                      <td className="px-6 py-4 text-sm text-gray-800">{l.to}</td>
                      <td className="px-6 py-4 text-sm text-gray-600 truncate max-w-48">{l.subject}</td>
                      <td className="px-6 py-4">
                        <span className={`px-2 py-1 text-xs rounded ${getStatusColor(l.status)}`}>
                          {l.status}
                        </span>
                      </td>
                      <td className="px-6 py-4 text-sm text-gray-500">
                        {new Date(l.sentDate).toLocaleString('tr-TR')}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
