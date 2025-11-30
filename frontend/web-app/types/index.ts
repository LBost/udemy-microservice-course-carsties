export type PagedResult<T> = {
  result: T[];
  pageCount: number;
  totalCount: number;
};

export type Auction = {
  reservePrice?: number;
  seller: string;
  winner?: string;
  soldAmount?: number;
  currentHighBid?: number;
  createdAt: string;
  updatedAt: string;
  auctionEnd: string;
  make: string;
  model: string;
  mileage: number;
  year: number;
  color: string;
  imageUrl: string;
  status: string;
  id: string;
};
