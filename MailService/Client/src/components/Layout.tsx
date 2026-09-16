import { Link, Outlet, useNavigate, useLocation } from 'react-router-dom'
import { useAuthStore } from '../store/authStore'
import DashboardIcon from '@mui/icons-material/Dashboard'
import KeyIcon from '@mui/icons-material/Key'
import ArticleIcon from '@mui/icons-material/Article'
import TimerIcon from '@mui/icons-material/Timer'
import DescriptionIcon from '@mui/icons-material/Description'
import LogoutIcon from '@mui/icons-material/Logout'
import MailIcon from '@mui/icons-material/Mail'

export default function Layout() {
  const navigate = useNavigate()
  const location = useLocation()
  const logout = useAuthStore((s) => s.logout)

  const handleLogout = () => {
    logout()
    navigate('/login')
  }

  const navItems = [
    { path: '/', label: 'Dashboard', icon: <DashboardIcon sx={{ fontSize: 20 }} /> },
    { path: '/clients', label: 'OAuth Clients', icon: <KeyIcon sx={{ fontSize: 20 }} /> },
    { path: '/templates', label: 'Templates', icon: <ArticleIcon sx={{ fontSize: 20 }} /> },
    { path: '/rate-limits', label: 'Rate Limits', icon: <TimerIcon sx={{ fontSize: 20 }} /> },
    { path: '/logs', label: 'Logs', icon: <DescriptionIcon sx={{ fontSize: 20 }} /> },
  ]

  const isActive = (path: string) => {
    if (path === '/') return location.pathname === '/'
    return location.pathname.startsWith(path)
  }

  return (
    <div className="min-h-screen bg-gray-100">
      <nav className="bg-white shadow-sm border-b">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16">
            <div className="flex items-center gap-2">
              <MailIcon className="text-blue-600" />
              <span className="text-xl font-bold text-blue-600"> Mail Admin</span>
            </div>
            <div className="flex items-center gap-2">
              <button
                onClick={handleLogout}
                className="flex items-center gap-2 text-sm text-gray-600 hover:text-red-600 font-medium px-3 py-2 rounded-lg hover:bg-red-50 transition-colors"
              >
                <LogoutIcon sx={{ fontSize: 18 }} />
                Çıkış
              </button>
            </div>
          </div>
        </div>
      </nav>

      <div className="flex">
        <aside className="w-56 bg-white shadow-sm min-h-screen border-r">
          <nav className="p-4 space-y-1">
            {navItems.map((item) => (
              <Link
                key={item.path}
                to={item.path}
                className={`flex items-center gap-3 px-4 py-3 rounded-lg text-sm font-medium transition-colors ${
                  isActive(item.path)
                    ? 'bg-blue-50 text-blue-700'
                    : 'text-gray-700 hover:bg-gray-50'
                }`}
              >
                {item.icon}
                {item.label}
              </Link>
            ))}
          </nav>
        </aside>

        <main className="flex-1 p-6">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
