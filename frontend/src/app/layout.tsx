import type { Metadata } from "next";
import { Inter } from "next/font/google";
import "./globals.css";
import { Sidebar } from "@/components/Sidebar";
import { Header } from "@/components/Header";
import { ThemeProvider } from "@/components/ThemeProvider";

const inter = Inter({ subsets: ["latin", "vietnamese"] });

export const metadata: Metadata = {
  title: "IQC QMS | Quality Management System",
  description: "Next-gen IQC Quality Management System",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="vi" suppressHydrationWarning>
      <body className={`${inter.className} antialiased text-foreground selection:bg-[#1428A0]/20 selection:text-[#1428A0] dark:selection:text-blue-400`}>
        <ThemeProvider attribute="class" defaultTheme="system" enableSystem disableTransitionOnChange={false}>
          
          {/* Animated Ambient Glow Background */}
          <div className="fixed inset-0 -z-10 h-full w-full bg-[#f8f9fa] dark:bg-[#030303] transition-colors duration-500">
            <div className="absolute top-0 right-0 -translate-y-12 translate-x-1/3 w-[800px] h-[800px] rounded-full bg-blue-400/10 dark:bg-blue-600/10 blur-[120px] pointer-events-none animate-pulse duration-[10000ms]" />
            <div className="absolute bottom-0 left-0 translate-y-1/3 -translate-x-1/3 w-[600px] h-[600px] rounded-full bg-indigo-400/10 dark:bg-indigo-600/10 blur-[100px] pointer-events-none" />
          </div>

          <div className="flex h-screen overflow-hidden p-4 gap-4">
            <Sidebar />
            <div className="flex-1 flex flex-col overflow-hidden gap-4">
              <Header />
              <main className="flex-1 overflow-y-auto rounded-[2rem] pb-8 pr-1 custom-scrollbar">
                {children}
              </main>
            </div>
          </div>

        </ThemeProvider>
      </body>
    </html>
  );
}
