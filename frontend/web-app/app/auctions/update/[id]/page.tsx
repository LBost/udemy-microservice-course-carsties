import { getAuctionDetails } from '@/app/actions/auctionActions';
import Headings from '@/app/components/Headings';
import AuctionForm from '../../AuctionForm';

export default async function Update({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const data = await getAuctionDetails(id);

  return (
    <div className="mx-auto max-w-[75%] shadow-lg p-10 bg-white rounded-lg">
      <Headings
        title="Update your auction"
        subTitle="Please update the details of your 
                car (only these auction properties can be updated)"
      />
      <AuctionForm auction={data} />
    </div>
  );
}
