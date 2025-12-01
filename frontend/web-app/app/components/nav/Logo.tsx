'use client';

import { useParamsStore } from '@/app/hooks/useParamsStore';
import { redirect } from 'next/navigation';
import { AiOutlineCar } from 'react-icons/ai';

export default function Logo() {
  const reset = useParamsStore((state) => state.reset);

  function goHome() {
    reset();
    redirect('/');
  }
  return (
    <div
      onClick={goHome}
      className="flex items-center gap-2 text-red-500 text-3xl font-semibold cursor-pointer"
    >
      <AiOutlineCar size={30} />
      <div>Carsties Auctions</div>
    </div>
  );
}
