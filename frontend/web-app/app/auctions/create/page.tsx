import Headings from '@/app/components/Headings';
import AuctionForm from '../AuctionForm';

export default function Create() {
  return (
    <div className="mx-auto max-w-[75%] shadow-lg p-10 bg-white rounded-lg">
      <Headings
        title="Sell your car"
        subTitle="Please enter the details of your car"
        centered
      ></Headings>
      <AuctionForm />
    </div>
  );
}
