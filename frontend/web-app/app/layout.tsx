import type { Metadata } from 'next';
import './globals.css';
import NavBar from './components/nav/NavBar';
import ToastProvider from './providers/ToastProvider';
import { ThemeProvider } from 'next-themes';

export const metadata: Metadata = {
  title: 'Carsties',
  description: 'The awesome car auction site',
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en" suppressHydrationWarning>
      <body className="dark:bg-gray-800 dark:text-gray-200">
        <ToastProvider />
        <NavBar></NavBar>
        <main className="container mx-auto px-5 pt-10">
          <ThemeProvider attribute="class" enableSystem defaultTheme="system">
            {children}
          </ThemeProvider>
        </main>
      </body>
    </html>
  );
}
