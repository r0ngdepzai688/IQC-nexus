import type { Metadata } from "next";
import { Inter } from "next/font/google";
import "./globals.css";
import { ThemeProvider } from "@/components/ThemeProvider";

const inter = Inter({ subsets: ["latin", "vietnamese"] });

import { LanguageProvider } from "@/lib/contexts/LanguageContext";

export const metadata: Metadata = {
  title: "IQC Quality Management Cloud",
  description: "Next-gen IQC Quality Management System",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="vi" suppressHydrationWarning>
      <body className={`${inter.className} antialiased text-foreground selection:bg-primary/20 selection:text-primary dark:selection:text-blue-400`}>
        <LanguageProvider>
          <ThemeProvider attribute="class" defaultTheme="system" enableSystem disableTransitionOnChange={false}>
            
            {/* Animated Ambient Glow Background */}
            <div className="fixed inset-0 -z-10 h-full w-full bg-[#f8f9fa] dark:bg-[#030303] transition-colors duration-500">
              <div className="absolute top-0 right-0 -translate-y-12 translate-x-1/3 w-[800px] h-[800px] rounded-full bg-blue-400/10 dark:bg-blue-600/10 blur-[120px] pointer-events-none animate-pulse duration-[10000ms]" />
              <div className="absolute bottom-0 left-0 translate-y-1/3 -translate-x-1/3 w-[600px] h-[600px] rounded-full bg-indigo-400/10 dark:bg-indigo-600/10 blur-[100px] pointer-events-none" />
            </div>

            {children}
          </ThemeProvider>
        </LanguageProvider>
      </body>
    </html>
  );
}
