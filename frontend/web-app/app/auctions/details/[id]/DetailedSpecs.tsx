'use client';

import { Auction } from '@/types';
import { Table, TableBody, TableCell, TableRow } from 'flowbite-react';

type Props = {
  auction: Auction;
};
export default function DetailedSpecs({ auction }: Props) {
  return (
    <Table striped={true}>
      <TableBody className="divide-y">
        <TableRow>
          <TableCell>Seller</TableCell>
          <TableCell>{auction.seller}</TableCell>
        </TableRow>
        <TableRow>
          <TableCell>Make</TableCell>
          <TableCell>{auction.make}</TableCell>
        </TableRow>
        <TableRow>
          <TableCell>Model</TableCell>
          <TableCell>{auction.model}</TableCell>
        </TableRow>
        <TableRow>
          <TableCell>Year manufactured</TableCell>
          <TableCell>{auction.year}</TableCell>
        </TableRow>
        <TableRow>
          <TableCell>Mileage</TableCell>
          <TableCell>{auction.mileage}</TableCell>
        </TableRow>
        <TableRow>
          <TableCell>Has reserve price?</TableCell>
          <TableCell>
            {auction.reservePrice && auction.reservePrice > 0 ? 'Yes' : 'No'}
          </TableCell>
        </TableRow>
      </TableBody>
    </Table>
  );
}
