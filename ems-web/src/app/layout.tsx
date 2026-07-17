import type { Metadata } from "next";
import { AuthProvider } from "@/providers/AuthProvider";
import { ProtectedShell } from "@/providers/ProtectedShell";
import { ReactQueryProvider } from "@/providers/ReactQueryProvider";
import { ThemeProvider } from "@/context/ThemeContext";
import { SidebarProvider } from "@/context/SidebarContext";
import { TooltipProvider } from "@/components/ui/tooltip";
import "./globals.css";

export const metadata: Metadata = {
  title: "EMS",
  description: "Employee Management System web client",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html
      lang="en"
      className="h-full antialiased"
      suppressHydrationWarning
    >
      <body className="flex min-h-full flex-col bg-background text-foreground">
        <ThemeProvider>
          <SidebarProvider>
            <TooltipProvider>
              <ReactQueryProvider>
                <AuthProvider>
                  <ProtectedShell>{children}</ProtectedShell>
                </AuthProvider>
              </ReactQueryProvider>
            </TooltipProvider>
          </SidebarProvider>
        </ThemeProvider>
      </body>
    </html>
  );
}
