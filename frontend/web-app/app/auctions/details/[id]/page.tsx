import { getAuctionDetails } from '@/app/actions/auctionActions';
import Headings from '@/app/components/Headings';
import CountdownTimer from '../../CountdownTimer';
import CarImage from '../../CarImage';
import DetailedSpecs from './DetailedSpecs';
import EditButton from './EditButton';
import { getCurrentUser } from '@/app/actions/authActions';
import DeleteButton from './DeleteButton';

export default async function Details({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const data = await getAuctionDetails(id);
  const user = await getCurrentUser();

  if (!data) return <div>Loading...</div>;

  return (
    <>
      <div className="flex justify-between">
        <Headings title={`${data.make} ${data.model}`} />
        {user?.username === data.seller && (
          <>
            <EditButton id={data.id} />
            <DeleteButton id={data.id} />
          </>
        )}
        <div className="flex gap-3">
          <h3 className="text-2xl font-semibold">Time remaining:</h3>
          <CountdownTimer auctionEnd={data.auctionEnd} />
        </div>
      </div>

      <div className="grid grid-cols-2 gap-6 mt-3">
        <div className="relative w-full bg-gray-200 aspect-4/3 rounded-lg overflow-hidden">
          <CarImage imageUrl={data.imageUrl} />
        </div>
        <div className="border rounded-lg p-2 bg-gray-200">
          <Headings title="Bids" />
        </div>

        <div className="mt-3">
          <DetailedSpecs auction={data} />
        </div>
      </div>
    </>
  );
}
