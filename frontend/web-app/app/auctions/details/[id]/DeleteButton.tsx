'use client';

import { deleteAuction } from '@/app/actions/auctionActions';
import { Button, Spinner } from 'flowbite-react';
import { useRouter } from 'next/navigation';
import { useState } from 'react';
import toast from 'react-hot-toast';

type Props = {
  id: string;
};

export default function DeleteButton({ id }: Props) {
  const [loading, setLoading] = useState(false);
  const router = useRouter();

  function handleDelete() {
    setLoading(true);
    deleteAuction(id)
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      .then((res: { error: any }) => {
        if (res.error) throw res.error;
        router.push('/');
      })
      .catch((error: { status: string; message: string }) => {
        toast.error(error.status + ' ' + error.message);
      })
      .finally(() => setLoading(false));
  }

  return (
    <Button outline color="red" onClick={handleDelete}>
      {loading && <Spinner size="sm" className="mr-3" />}
      Delete Auction
    </Button>
  );
}
