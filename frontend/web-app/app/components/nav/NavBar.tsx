import Search from './Search';
import Logo from './Logo';
import Login from './Login';
import { getCurrentUser } from '@/app/actions/authActions';
import SessionInfo from './SessionInfo';

export default async function NavBar() {
  const user = await getCurrentUser();
  return (
    <header className="sticky top-0 z-50 flex justify-between bg-white p-5 items-center text-gray-800 shadow-md">
      <Logo />
      <Search />
      {user ? <SessionInfo user={user} /> : <Login />}
    </header>
  );
}
