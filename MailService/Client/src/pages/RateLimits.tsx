import { useState, useEffect } from 'react'
import { rateLimitsApi, ClientApplication, CreateClientApp } from '../services/api'

export default function RateLimits() {
  const [clients, setClients] = useState<ClientApplication[]>([])
  const [loading, setLoading] = useState(true)
  const [showModal, setShowModal] = useState(false)
  const [editClient, setEditClient] = useState<ClientApplication | null>(null)
  const [form, setForm] = useState<CreateClientApp>({
    clientId: '',
    name: '',
    dailyLimit: 1000,
    minuteLimit: 100,
  })

  useEffect(() => {
    loadClients()
  }, [])

  const loadClients = async () => {
    setLoading(true)
    try {
      const res = await rateLimitsApi.getAll()
      setClients(res.data)
    } catch (err) {
      console.error(err)
    } finally {
      setLoading(false)
    }
  }

  const openCreate = () => {
    setEditClient(null)
    setForm({ clientId: '', name: '', dailyLimit: 1000, minuteLimit: 100 })
    setShowModal(true)
  }

  const openEdit = (c: ClientApplication) => {
    setEditClient(c)
    setForm({
      clientId: c.clientId,
      name: c.name,
      dailyLimit: c.dailyLimit,
      minuteLimit: c.minuteLimit,
    })
    setShowModal(true)
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      if (editClient) {
        await rateLimitsApi.update(editClient.clientId, form)
      } else {
        await rateLimitsApi.create(form)
      }
      setShowModal(false)
      loadClients()
    } catch (err) {
      console.error(err)
    }
  }

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-800">Rate Limits</h1>
        <button
          onClick={openCreate}
          className="bg-blue-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-blue-700"
        >
          + Yeni Limit
        </button>
      </div>

      <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 mb-6">
        <p className="text-sm text-blue-700">
          <strong>Not:</strong> Buradaki ClientId, OAuth ClientId ile eşleşmeli.
          Örn: OAuth'da "crm-api" varsa, burada da "crm-api" olmalı.
        </p>
      </div>

      {loading ? (
        <div className="text-gray-500 text-center py-12">Yükleniyor...</div>
      ) : clients.length === 0 ? (
        <div className="bg-white p-8 rounded-xl shadow-sm border text-center">
          <p className="text-gray-500">Henüz rate limit tanımlanmamış</p>
          <p className="text-gray-400 text-sm mt-1">
            Varsayılan limit: Günlük 100, Dakikalık 10
          </p>
        </div>
      ) : (
        <div className="bg-white rounded-xl shadow-sm border overflow-hidden">
          <table className="w-full">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                  Client ID
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                  Name
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                  Günlük Limit
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                  Dakikalık Limit
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                  İşlem
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200">
              {clients.map((c) => (
                <tr key={c.id} className="hover:bg-gray-50">
                  <td className="px-6 py-4 text-sm font-mono text-gray-800">
                    {c.clientId}
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-800">{c.name}</td>
                  <td className="px-6 py-4">
                    <span className="text-sm font-medium text-blue-600">
                      {c.dailyLimit.toLocaleString()}
                    </span>
                  </td>
                  <td className="px-6 py-4">
                    <span className="text-sm font-medium text-purple-600">
                      {c.minuteLimit.toLocaleString()}
                    </span>
                  </td>
                  <td className="px-6 py-4">
                    <button
                      onClick={() => openEdit(c)}
                      className="text-sm text-blue-600 hover:text-blue-800"
                    >
                      Düzenle
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {showModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-xl p-6 w-full max-w-md">
            <h2 className="text-lg font-bold text-gray-800 mb-4">
              {editClient ? 'Limit Düzenle' : 'Yeni Limit'}
            </h2>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Client ID
                </label>
                <input
                  type="text"
                  value={form.clientId}
                  onChange={(e) => setForm({ ...form, clientId: e.target.value })}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none font-mono"
                  placeholder="crm-api"
                  disabled={!!editClient}
                  required
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Name</label>
                <input
                  type="text"
                  value={form.name}
                  onChange={(e) => setForm({ ...form, name: e.target.value })}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none"
                  placeholder="CRM Uygulaması"
                  required
                />
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Günlük Limit
                  </label>
                  <input
                    type="number"
                    value={form.dailyLimit}
                    onChange={(e) => setForm({ ...form, dailyLimit: parseInt(e.target.value) || 0 })}
                    className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none"
                    min={0}
                    required
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Dakikalık Limit
                  </label>
                  <input
                    type="number"
                    value={form.minuteLimit}
                    onChange={(e) => setForm({ ...form, minuteLimit: parseInt(e.target.value) || 0 })}
                    className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none"
                    min={0}
                    required
                  />
                </div>
              </div>
              <div className="flex justify-end gap-3 pt-4">
                <button
                  type="button"
                  onClick={() => setShowModal(false)}
                  className="px-4 py-2 text-gray-700 hover:bg-gray-100 rounded-lg text-sm"
                >
                  İptal
                </button>
                <button
                  type="submit"
                  className="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm hover:bg-blue-700"
                >
                  Kaydet
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
