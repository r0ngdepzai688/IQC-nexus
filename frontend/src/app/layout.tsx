import type { Metadata } from "next";
import { Inter } from "next/font/google";
import "./globals.css";
import { Sidebar } from "@/components/Sidebar";
import { Header } from "@/components/Header";
import { ThemeProvider } from "@/components/ThemeProvider";

const inter = Inter({ subsets: ["latin", "vietnamese"] });

export const metadata: Metadata = {
  title: "IQC Portal - Samsung",
  description: "Incoming Quality Control Management System",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="vi" suppressHydrationWarning>
      <body className={`${inter.className} bg-slate-50 dark:bg-[#09090b] antialiased flex h-screen overflow-hidden transition-colors duration-300`}>
        <ThemeProvider attribute="class" defaultTheme="system" enableSystem disableTransitionOnChange={false}>
          <Sidebar />
          <div className="flex-1 flex flex-col h-screen overflow-hidden">
            <Header />
            <main className="flex-1 overflow-y-auto p-8">
              {children}
            </main>
          </div>
        </ThemeProvider>
      </body>
    </html>
  );
}
