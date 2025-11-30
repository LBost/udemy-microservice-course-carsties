'use server';

import { Auction, PagedResult } from '@/types';

export async function getData(query: string): Promise<PagedResult<Auction>> {
  try {
    const res = await fetch(`http://localhost:6001/search${query}`);
    return res.json();
  } catch (error) {
    throw error;
  }
}
