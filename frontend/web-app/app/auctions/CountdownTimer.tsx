'use client';

import Countdown, { zeroPad } from 'react-countdown';

const renderer = ({
  days,
  hours,
  minutes,
  seconds,
  completed,
}: {
  days: number;
  hours: number;
  minutes: number;
  seconds: number;
  completed: boolean;
}) => {
  return (
    <div
      className={`
            border  text-white py-1 px-2 rounded-lg flex justify-center opacity-80 
            ${
              completed
                ? 'border-red-500'
                : days === 0 && hours < 10
                ? 'border-amber-500'
                : 'border-green-500'
            }
            ${
              completed
                ? 'bg-red-500'
                : days === 0 && hours < 10
                ? 'bg-amber-500'
                : 'bg-green-500'
            }`}
    >
      {completed ? (
        <span>Auction finished</span>
      ) : (
        <span suppressHydrationWarning={true}>
          {days >= 1 ? zeroPad(days) : null}{' '}
          {days > 1 ? 'days' : days === 1 ? 'day' : null} {zeroPad(hours, 2)}:
          {zeroPad(minutes, 2)}:{zeroPad(seconds, 2)}
        </span>
      )}
    </div>
  );
};

type Props = {
  auctionEnd: string;
};

export default function CountdownTimer({ auctionEnd }: Props) {
  return (
    <div>
      <Countdown date={auctionEnd} renderer={renderer} />
    </div>
  );
}
