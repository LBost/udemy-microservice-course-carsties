'use client';

import { useSearchParams } from 'next/navigation';
import RedirectLogin from '@/app/components/RedirectLogin';

export default function SignIn() {
  const searchParams = useSearchParams();
  return (
    <RedirectLogin callbackUrl={searchParams.get('callbackUrl')} showLogin />
  );
}
