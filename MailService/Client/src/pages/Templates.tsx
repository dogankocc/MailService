import { useState, useEffect } from 'react'
import { templatesApi, MailTemplate, CreateTemplate } from '../services/api'

export default function Templates() {
  const [templates, setTemplates] = useState<MailTemplate[]>([])
  const [loading, setLoading] = useState(true)
  const [showModal, setShowModal] = useState(false)
  const [editTemplate, setEditTemplate] = useState<MailTemplate | null>(null)
  const [form, setForm] = useState<CreateTemplate>({
    code: '',
    name: '',
    subject: '',
    body: '',
  })

  useEffect(() => {
    loadTemplates()
  }, [])

  const loadTemplates = async () => {
    setLoading(true)
    try {
      const res = await templatesApi.getAll()
      setTemplates(res.data)
    } catch (err) {
      console.error(err)
    } finally {
      setLoading(false)
    }
  }

  const openCreate = () => {
    setEditTemplate(null)
    setForm({ code: '', name: '', subject: '', body: '' })
    setShowModal(true)
  }

  const openEdit = (t: MailTemplate) => {
    setEditTemplate(t)
    setForm({
      code: t.code,
      name: t.name,
      subject: t.subject,
      body: t.body,
    })
    setShowModal(true)
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      if (editTemplate) {
        await templatesApi.update(editTemplate.code, form)
      } else {
        await templatesApi.create(form)
      }
      setShowModal(false)
      loadTemplates()
    } catch (err) {
      console.error(err)
    }
  }

  const handleDelete = async (code: string) => {
    if (!confirm('Silmek istediğinize emin misiniz?')) return
    try {
      await templatesApi.delete(code)
      loadTemplates()
    } catch (err) {
      console.error(err)
    }
  }

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-800">Templates</h1>
        <button
          onClick={openCreate}
          className="bg-blue-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-blue-700"
        >
          + Yeni Template
        </button>
      </div>

      {loading ? (
        <div className="text-gray-500 text-center py-12">Yükleniyor...</div>
      ) : templates.length === 0 ? (
        <div className="bg-white p-8 rounded-xl shadow-sm border text-center">
          <p className="text-gray-500">Henüz template oluşturulmamış</p>
        </div>
      ) : (
        <div className="space-y-4">
          {templates
            .filter((t) => t.isActive)
            .map((t) => (
              <div key={t.id} className="bg-white p-6 rounded-xl shadow-sm border">
                <div className="flex items-start justify-between">
                  <div>
                    <div className="flex items-center gap-3 mb-2">
                      <span className="font-mono text-sm bg-blue-100 text-blue-700 px-2 py-1 rounded">
                        {t.code}
                      </span>
                      <span className="text-lg font-semibold text-gray-800">{t.name}</span>
                      <span className="text-xs bg-gray-100 text-gray-600 px-2 py-1 rounded">
                        v{t.version}
                      </span>
                    </div>
                    <p className="text-sm text-gray-600 mb-1">
                      <strong>Subject:</strong> {t.subject}
                    </p>
                    <div className="mt-3 bg-gray-50 p-3 rounded-lg">
                      <p className="text-xs text-gray-500 mb-1">Body (ilk 200 karakter):</p>
                      <p className="text-sm text-gray-700 font-mono whitespace-pre-wrap">
                        {t.body.length > 200 ? t.body.substring(0, 200) + '...' : t.body}
                      </p>
                    </div>
                  </div>
                  <div className="flex gap-2">
                    <button
                      onClick={() => openEdit(t)}
                      className="text-sm text-blue-600 hover:text-blue-800"
                    >
                      Düzenle
                    </button>
                    <button
                      onClick={() => handleDelete(t.code)}
                      className="text-sm text-red-600 hover:text-red-800"
                    >
                      Sil
                    </button>
                  </div>
                </div>
              </div>
            ))}
        </div>
      )}

      {showModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-xl p-6 w-full max-w-2xl max-h-screen overflow-y-auto">
            <h2 className="text-lg font-bold text-gray-800 mb-4">
              {editTemplate ? 'Template Düzenle' : 'Yeni Template'}
            </h2>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Code</label>
                <input
                  type="text"
                  value={form.code}
                  onChange={(e) => setForm({ ...form, code: e.target.value })}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none font-mono"
                  placeholder="PASSWORD_RESET"
                  disabled={!!editTemplate}
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
                  placeholder="Şifre Sıfırlama"
                  required
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Subject</label>
                <input
                  type="text"
                  value={form.subject}
                  onChange={(e) => setForm({ ...form, subject: e.target.value })}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none"
                  placeholder="Merhaba {{Name}}, Şifrenizi Sıfırlayın"
                  required
                />
              </div>
              <div>
                <textarea
                  value={form.body}
                  onChange={(e) => setForm({ ...form, body: e.target.value })}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none font-mono text-sm"
                  rows={10}
                  placeholder={`<p>Merhaba {{Name}},</p>
<p>Şifrenizi sıfırlamak için tıklayın:</p>
<p><a href="{{ResetLink}}">{{ResetLink}}</a></p>`}
                  required
                />
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
