'use client';

import { Button } from 'flowbite-react';
import Headings from './Headings';
import { signIn } from 'next-auth/react';

type Props = {
  title?: string;
  subTitle?: string;
  showLogin: boolean;
  callbackUrl: string | null;
};

export default function RedirectLogin({
  title = 'Authentication required',
  subTitle = 'You are trying to access a protected page, please login.',
  showLogin,
  callbackUrl,
}: Props) {
  return (
    <div className="flex flex-col items-center justify-center h-[40vh] shadow-lg">
      <Headings title={title} subTitle={subTitle} centered />
      <div className="mt-4">
        {showLogin && (
          <Button
            onClick={() =>
              signIn('id-server', { redirectTo: callbackUrl ?? '/' })
            }
          >
            Login
          </Button>
        )}
      </div>
    </div>
  );
}
