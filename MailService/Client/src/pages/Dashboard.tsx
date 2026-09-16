import EmailIcon from '@mui/icons-material/Email'
import CheckCircleIcon from '@mui/icons-material/CheckCircle'
import CancelIcon from '@mui/icons-material/Cancel'
import KeyIcon from '@mui/icons-material/Key'
import AddCircleIcon from '@mui/icons-material/AddCircle'
import SettingsIcon from '@mui/icons-material/Settings'
import ArticleIcon from '@mui/icons-material/Article'

export default function Dashboard() {
  return (
    <div>
      <h1 className="text-2xl font-bold text-gray-800 mb-6">Dashboard</h1>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
        <StatCard title="Toplam Mail" value="0" icon={<EmailIcon sx={{ fontSize: 28 }} />} color="bg-blue-500" />
        <StatCard title="Başarılı" value="0" icon={<CheckCircleIcon sx={{ fontSize: 28 }} />} color="bg-green-500" />
        <StatCard title="Başarısız" value="0" icon={<CancelIcon sx={{ fontSize: 28 }} />} color="bg-red-500" />
        <StatCard title="Aktif Client" value="3" icon={<KeyIcon sx={{ fontSize: 28 }} />} color="bg-purple-500" />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div className="bg-white p-6 rounded-xl shadow-sm border">
          <h2 className="text-lg font-semibold text-gray-800 mb-4">Hızlı İşlemler</h2>
          <div className="space-y-3">
            <QuickAction label="Yeni Template Ekle" path="/templates" icon={<ArticleIcon sx={{ fontSize: 18 }} />} />
            <QuickAction label="Yeni Client Ekle" path="/clients" icon={<AddCircleIcon sx={{ fontSize: 18 }} />} />
            <QuickAction label="Rate Limit Ayarla" path="/rate-limits" icon={<SettingsIcon sx={{ fontSize: 18 }} />} />
          </div>
        </div>

        <div className="bg-white p-6 rounded-xl shadow-sm border">
          <h2 className="text-lg font-semibold text-gray-800 mb-4">Kuyruk Durumu</h2>
          <div className="space-y-3">
            <div className="flex items-center justify-between p-3 bg-gray-50 rounded-lg">
              <span className="text-sm text-gray-600">Queue Type</span>
              <span className="text-sm font-medium text-gray-800">Kafka / RabbitMQ</span>
            </div>
            <div className="flex items-center justify-between p-3 bg-gray-50 rounded-lg">
              <span className="text-sm text-gray-600">Bekleyen Mesaj</span>
              <span className="text-sm font-medium text-blue-600">-</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}

function StatCard({
  title,
  value,
  icon,
  color,
}: {
  title: string
  value: string
  icon: React.ReactNode
  color: string
}) {
  return (
    <div className="bg-white p-6 rounded-xl shadow-sm border">
      <div className="flex items-center justify-between">
        <div>
          <p className="text-sm text-gray-500">{title}</p>
          <p className="text-2xl font-bold text-gray-800 mt-1">{value}</p>
        </div>
        <div className={`${color} p-3 rounded-lg text-white`}>
          {icon}
        </div>
      </div>
    </div>
  )
}

function QuickAction({ label, path, icon }: { label: string; path: string; icon: React.ReactNode }) {
  const goTo = () => {
    window.location.href = path
  }
  return (
    <button
      onClick={goTo}
      className="w-full flex items-center gap-3 p-3 bg-gray-50 hover:bg-blue-50 rounded-lg transition-colors text-left"
    >
      <span className="text-gray-600">{icon}</span>
      <span className="text-sm font-medium text-gray-700">{label}</span>
      <span className="ml-auto text-gray-400">→</span>
    </button>
  )
}
