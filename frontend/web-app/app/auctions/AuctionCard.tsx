import CountdownTimer from './CountdownTimer';
import CarImage from './CarImage';
import { Auction } from '@/types';

type Props = {
  auction: Auction;
};

export default function AuctionCard({ auction }: Props) {
  return (
    <a
      href={`/auctions/details/${auction.id}`}
      className="border border-gray-200 rounded-lg dark:bg-gray-800 dark:text-gray-200 dark:border-gray-700"
    >
      <div className="relative w-full bg-gray-200 aspect-video rounded-lg overflow-hidden dark:bg-gray-800 dark:text-gray-200">
        <CarImage imageUrl={auction.imageUrl} />
        <div className="absolute bottom-1 left-1">
          <CountdownTimer auctionEnd={auction.auctionEnd} />
        </div>
      </div>
      <div className="flex justify-between items-center mt-4 px-2">
        <h3 className="">
          {auction.make} {auction.model}
        </h3>
        <h3 className="font-semibold">{auction.year}</h3>
      </div>
    </a>
  );
}
