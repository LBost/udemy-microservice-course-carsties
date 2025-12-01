'use server';

import { auth } from '@/auth';
import { Auction, PagedResult } from '@/types';

export async function getData(query: string): Promise<PagedResult<Auction>> {
  try {
    const res = await fetch(`http://localhost:6001/search${query}`);
    return res.json();
  } catch (error) {
    throw error;
  }
}

export async function updateAuctionTest(): Promise<{
  status: number;
  message: string;
}> {
  const data = {
    mileage: Math.floor(Math.random() * 1000) + 1,
  };
  const session = await auth();

  const result = await fetch(
    'http://localhost:6001/auctions/afbee524-5972-4075-8800-7d1f9d7b0a0c',
    {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${session?.accessToken}`,
      },
      body: JSON.stringify(data),
    }
  );

  return { status: result.status, message: result.statusText };
}
