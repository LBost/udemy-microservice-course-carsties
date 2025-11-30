import CountdownTimer from './CountdownTimer';
import CarImage from './CarImage';
import { Auction } from '@/types';

type Props = {
  auction: Auction;
};

export default function AuctionCard({ auction }: Props) {
  return (
    <a href="#" className="border border-gray-200 rounded-lg">
      <div className="relative w-full bg-gray-200 aspect-video rounded-lg overflow-hidden">
        <CarImage imageUrl={auction.imageUrl} />
        <div className="absolute bottom-1 left-1">
          <CountdownTimer auctionEnd={auction.auctionEnd} />
        </div>
      </div>
      <div className="flex justify-between items-center mt-4 px-2">
        <h3 className="text-gray-700">
          {auction.make} {auction.model}
        </h3>
        <h3 className="text-gray-900 font-semibold">{auction.year}</h3>
      </div>
    </a>
  );
}
