'use client';

import { Button, Spinner } from 'flowbite-react';
import { usePathname, useRouter } from 'next/navigation';
import { FieldValues, useForm } from 'react-hook-form';
import Input from '../components/Input';
import { useEffect } from 'react';
import { createAuction, updateAuction } from '../actions/auctionActions';
import DateInput from '../components/DateInput';
import toast from 'react-hot-toast';
import { Auction } from '@/types';

type Props = {
  auction?: Auction;
};

export default function AuctionForm({ auction }: Props) {
  const router = useRouter();
  const pathname = usePathname();
  const {
    control,
    handleSubmit,
    setFocus,
    reset,
    formState: { isSubmitting, isValid, isDirty },
  } = useForm({ mode: 'onTouched' });

  useEffect(() => {
    if (auction) {
      const { make, model, color, mileage, year } = auction;
      reset({ make, model, color, mileage, year });
    }
    setFocus('make');
  }, [setFocus, auction, reset]);

  async function onSubmit(data: FieldValues) {
    try {
      let id = '';
      let res;
      if (pathname === '/auctions/create') {
        res = await createAuction(data);
        id = res.id;
      } else {
        if (auction) {
          res = await updateAuction(data, auction.id);
          id = auction.id;
        }
      }
      if (res.error) {
        throw res.error;
      }
      router.push(`/auctions/details/${id}`);
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
    } catch (error: any) {
      toast.error(error.status + ' ' + error.message);
    }
  }

  return (
    <form className="flex flex-col mt-3" onSubmit={handleSubmit(onSubmit)}>
      <Input
        name="make"
        label="Make"
        control={control}
        rules={{ required: 'Make is required' }}
      />
      <Input
        name="model"
        label="Model"
        control={control}
        rules={{ required: 'Model is required' }}
      />
      <Input
        name="color"
        label="Color"
        control={control}
        rules={{ required: 'Color is required' }}
      />
      <Input
        name="imageUrl"
        label="Image URL"
        control={control}
        rules={{ required: 'Image URL is required' }}
      />
      <div className="grid grid-cols-2 gap-3">
        <Input
          name="year"
          label="Year"
          type="number"
          control={control}
          rules={{ required: 'Year is required' }}
        />
        <Input
          name="mileage"
          label="Mileage"
          type="number"
          control={control}
          rules={{ required: 'Mileage is required' }}
        />
        <Input
          name="reservePrice"
          label="Reserve Price"
          type="number"
          control={control}
          rules={{ required: 'Reserve price is required' }}
        />
        <DateInput
          name="auctionEnd"
          label="Auction end date"
          type="datetime-local"
          control={control}
          showTimeSelect
          dateFormat="dd MMMM yyyy HH:mm"
          rules={{ required: 'Auction end date is required' }}
        />
      </div>
      <div className="flex justify-end gap-4">
        <Button
          outline
          color="success"
          type="submit"
          disabled={!isValid || !isDirty}
        >
          {isSubmitting && <Spinner />}
          Submit
        </Button>
        <Button outline color="gray" onClick={() => router.push('/')}>
          Cancel
        </Button>
      </div>
    </form>
  );
}
