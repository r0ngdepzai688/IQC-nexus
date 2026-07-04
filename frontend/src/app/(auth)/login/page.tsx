"use client";

import { useState, useEffect } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { Fingerprint, Eye, EyeOff, Lock, User, ArrowRight, ShieldCheck, Zap } from "lucide-react";
import { useRouter } from "next/navigation";

export default function LoginPage() {
  const [employeeId, setEmployeeId] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [isBiometric, setIsBiometric] = useState(false);
  const router = useRouter();

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!employeeId || !password) return;
    
    setIsLoading(true);
    try {
      const res = await fetch("http://localhost:5215/api/auth/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ username: employeeId, password })
      });

      const data = await res.json();
      
      if (!res.ok) {
        alert(data.message || "Đăng nhập thất bại");
        setIsLoading(false);
        return;
      }

      // Save token and user info
      localStorage.setItem("token", data.token);
      localStorage.setItem("user", JSON.stringify(data.user));
      
      router.push("/");
    } catch (err) {
      console.error(err);
      alert("Lỗi kết nối đến máy chủ.");
      setIsLoading(false);
    }
  };

  const handleBiometricLogin = () => {
    setIsBiometric(true);
    setTimeout(() => {
      setIsBiometric(false);
      router.push("/");
    }, 2000);
  };

  return (
    <div className="min-h-screen w-full flex bg-[#0A0A0A] overflow-hidden text-white relative font-sans">
      
      {/* LEFT PANEL - Branding & Graphic (Hidden on mobile) */}
      <div className="hidden lg:flex lg:w-1/2 relative flex-col justify-between p-12 overflow-hidden border-r border-white/10 bg-gradient-to-br from-[#050B24] via-[#0A0A0A] to-[#0A0A0A]">
        {/* Dynamic Abstract Shapes using CSS */}
        <div className="absolute top-0 left-0 w-full h-full overflow-hidden pointer-events-none">
          <motion.div 
            animate={{ 
              rotate: 360,
              scale: [1, 1.2, 1],
            }}
            transition={{ duration: 30, repeat: Infinity, ease: "linear" }}
            className="absolute -top-[20%] -left-[10%] w-[800px] h-[800px] rounded-full bg-[#1428A0]/20 blur-[120px]"
          />
          <motion.div 
            animate={{ 
              rotate: -360,
              scale: [1, 1.5, 1],
            }}
            transition={{ duration: 40, repeat: Infinity, ease: "linear" }}
            className="absolute bottom-[10%] -right-[20%] w-[600px] h-[600px] rounded-full bg-blue-600/10 blur-[100px]"
          />
        </div>

        {/* Logo */}
        <motion.div 
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.8 }}
          className="relative z-10 flex items-center space-x-3"
        >
          <div className="w-12 h-12 bg-white rounded-2xl flex items-center justify-center shadow-lg shadow-white/10 overflow-hidden">
            <img src="/logo.png" alt="Logo" className="w-full h-full object-cover" />
          </div>
          <div>
            <h1 className="text-2xl font-black tracking-tight text-white">IQC QMS</h1>
            <p className="text-sm font-semibold text-blue-400">Quality Management Cloud</p>
          </div>
        </motion.div>

        {/* Hero Text */}
        <motion.div 
          initial={{ opacity: 0, x: -30 }}
          animate={{ opacity: 1, x: 0 }}
          transition={{ duration: 1, delay: 0.3 }}
          className="relative z-10 max-w-lg mt-20"
        >
          <h2 className="text-5xl font-black leading-[1.1] tracking-tighter mb-6 bg-clip-text text-transparent bg-gradient-to-r from-white to-gray-500">
            Kỷ nguyên mới<br/>của Quản lý Chất lượng.
          </h2>
          <p className="text-lg text-gray-400 font-medium leading-relaxed">
            Hệ thống thông minh, tích hợp AI, tối ưu hóa quy trình kiểm tra và giám sát chất lượng sản phẩm theo tiêu chuẩn khắt khe nhất của Samsung.
          </p>
          
          <div className="mt-12 flex items-center space-x-6">
            <div className="flex -space-x-4">
              {[1,2,3,4].map(i => (
                <div key={i} className="w-10 h-10 rounded-full border-2 border-[#0A0A0A] bg-gray-800 flex items-center justify-center text-xs font-bold text-gray-400">
                  <User className="w-4 h-4" />
                </div>
              ))}
            </div>
            <p className="text-sm font-semibold text-gray-400">
              Hơn <span className="text-white">5,000+</span> kỹ sư đang sử dụng.
            </p>
          </div>
        </motion.div>

        {/* Footer info */}
        <motion.div 
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ delay: 0.8, duration: 1 }}
          className="relative z-10 text-xs text-gray-500 font-medium"
        >
          &copy; 2026 Samsung Electronics. All rights reserved.<br/>
          Secured by Knox Enterprise.
        </motion.div>
      </div>

      {/* RIGHT PANEL - Login Form */}
      <div className="w-full lg:w-1/2 flex items-center justify-center p-6 sm:p-12 relative">
        {/* Mobile Logo */}
        <div className="absolute top-8 left-8 lg:hidden flex items-center space-x-3">
          <div className="w-10 h-10 bg-white rounded-xl flex items-center justify-center overflow-hidden">
            <img src="/logo.png" alt="Logo" className="w-full h-full object-cover" />
          </div>
          <h1 className="text-xl font-black tracking-tight">IQC QMS</h1>
        </div>

        <motion.div 
          initial={{ opacity: 0, scale: 0.95, y: 20 }}
          animate={{ opacity: 1, scale: 1, y: 0 }}
          transition={{ duration: 0.6, type: "spring", bounce: 0.4 }}
          className="w-full max-w-[420px]"
        >
          {/* Header */}
          <div className="text-center mb-10">
            <h2 className="text-3xl font-black tracking-tight mb-2">Đăng nhập</h2>
            <p className="text-gray-400 text-sm font-medium">Nhập thông tin xác thực của bạn để tiếp tục</p>
          </div>

          <form onSubmit={handleLogin} className="space-y-5">
            {/* Employee ID Input */}
            <div className="relative group">
              <input
                type="text"
                id="employeeId"
                value={employeeId}
                onChange={(e) => setEmployeeId(e.target.value)}
                className={`w-full bg-white/5 border border-white/10 rounded-2xl px-4 pt-6 pb-2 text-white font-medium outline-none transition-all duration-300 focus:border-[#1428A0] focus:bg-white/10 peer ${employeeId ? 'bg-white/10' : ''}`}
                required
              />
              <label 
                htmlFor="employeeId" 
                className={`absolute left-4 transition-all duration-300 pointer-events-none flex items-center text-gray-400 peer-focus:text-blue-400 peer-focus:text-xs peer-focus:top-2 peer-focus:-translate-y-0 ${
                  employeeId ? 'text-xs top-2' : 'text-sm top-1/2 -translate-y-1/2'
                }`}
              >
                <User className="w-3.5 h-3.5 mr-1.5" />
                Mã nhân viên (Knox ID)
              </label>
            </div>

            {/* Password Input */}
            <div className="relative group">
              <input
                type={showPassword ? "text" : "password"}
                id="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className={`w-full bg-white/5 border border-white/10 rounded-2xl px-4 pt-6 pb-2 text-white font-medium outline-none transition-all duration-300 focus:border-[#1428A0] focus:bg-white/10 peer ${password ? 'bg-white/10' : ''}`}
                required
              />
              <label 
                htmlFor="password" 
                className={`absolute left-4 transition-all duration-300 pointer-events-none flex items-center text-gray-400 peer-focus:text-blue-400 peer-focus:text-xs peer-focus:top-2 peer-focus:-translate-y-0 ${
                  password ? 'text-xs top-2' : 'text-sm top-1/2 -translate-y-1/2'
                }`}
              >
                <Lock className="w-3.5 h-3.5 mr-1.5" />
                Mật khẩu
              </label>
              
              <button 
                type="button"
                onClick={() => setShowPassword(!showPassword)}
                className="absolute right-4 top-1/2 -translate-y-1/2 text-gray-400 hover:text-white transition-colors"
                tabIndex={-1}
              >
                {showPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
              </button>
            </div>

            {/* Options */}
            <div className="flex items-center justify-between mt-4">
              <label className="flex items-center space-x-2 cursor-pointer group">
                <div className="relative w-4 h-4 rounded border border-gray-500 group-hover:border-blue-400 flex items-center justify-center transition-colors">
                  <input type="checkbox" className="absolute opacity-0 w-full h-full cursor-pointer peer" />
                  <div className="w-2 h-2 rounded-sm bg-blue-500 scale-0 peer-checked:scale-100 transition-transform"></div>
                </div>
                <span className="text-xs font-semibold text-gray-400 group-hover:text-gray-300 transition-colors">Ghi nhớ đăng nhập</span>
              </label>
              
              <a href="#" className="text-xs font-semibold text-blue-400 hover:text-blue-300 hover:underline transition-all">Quên mật khẩu?</a>
            </div>

            {/* Login Button */}
            <button 
              type="submit" 
              disabled={isLoading || !employeeId || !password}
              className="w-full relative h-12 bg-white text-black font-bold rounded-2xl overflow-hidden group disabled:opacity-50 disabled:cursor-not-allowed mt-6 transition-transform active:scale-[0.98]"
            >
              <AnimatePresence mode="wait">
                {isLoading ? (
                  <motion.div 
                    key="loading" 
                    initial={{ opacity: 0 }} 
                    animate={{ opacity: 1 }} 
                    exit={{ opacity: 0 }} 
                    className="absolute inset-0 flex items-center justify-center"
                  >
                    <div className="w-5 h-5 border-2 border-black/20 border-t-black rounded-full animate-spin" />
                  </motion.div>
                ) : (
                  <motion.div 
                    key="text" 
                    initial={{ opacity: 0 }} 
                    animate={{ opacity: 1 }} 
                    exit={{ opacity: 0 }}
                    className="absolute inset-0 flex items-center justify-center space-x-2"
                  >
                    <span>Đăng nhập</span>
                    <ArrowRight className="w-4 h-4 group-hover:translate-x-1 transition-transform" />
                  </motion.div>
                )}
              </AnimatePresence>
            </button>
          </form>

          {/* Divider */}
          <div className="flex items-center my-8">
            <div className="flex-1 h-px bg-white/10"></div>
            <span className="px-4 text-xs font-bold text-gray-500 uppercase tracking-wider">Hoặc tiếp tục với</span>
            <div className="flex-1 h-px bg-white/10"></div>
          </div>

          {/* Alternative Login Methods */}
          <div className="grid grid-cols-2 gap-4">
            {/* SSO Knox Button */}
            <button className="flex items-center justify-center space-x-2 h-12 bg-white/5 hover:bg-white/10 border border-white/10 rounded-2xl transition-all group">
              <ShieldCheck className="w-5 h-5 text-blue-400 group-hover:scale-110 transition-transform" />
              <span className="text-sm font-bold text-gray-300">Knox SSO</span>
            </button>
            
            {/* Biometric Button */}
            <button 
              onClick={handleBiometricLogin}
              className="flex items-center justify-center space-x-2 h-12 bg-[#1428A0]/10 hover:bg-[#1428A0]/20 border border-[#1428A0]/30 rounded-2xl transition-all group relative overflow-hidden"
            >
              {isBiometric ? (
                <div className="w-5 h-5 border-2 border-blue-400/30 border-t-blue-400 rounded-full animate-spin" />
              ) : (
                <>
                  <Fingerprint className="w-5 h-5 text-blue-400 group-hover:scale-110 transition-transform" />
                  <span className="text-sm font-bold text-blue-400">Passkey</span>
                </>
              )}
              {/* Scan effect */}
              <div className="absolute top-0 left-0 w-full h-0.5 bg-blue-400/50 blur-[2px] opacity-0 group-hover:opacity-100 group-hover:animate-scan pointer-events-none" />
            </button>
          </div>

          {/* Warning text */}
          <div className="mt-8 text-center bg-yellow-500/10 border border-yellow-500/20 rounded-xl p-3 flex items-start space-x-3">
            <Zap className="w-4 h-4 text-yellow-500 flex-shrink-0 mt-0.5" />
            <p className="text-[10px] text-yellow-500/80 font-medium text-left leading-relaxed">
              Hệ thống lưu trữ thông tin tuyệt mật. Truy cập trái phép sẽ bị theo dõi và xử lý theo quy định bảo mật của công ty.
            </p>
          </div>

        </motion.div>
      </div>

      <style dangerouslySetInnerHTML={{__html: `
        @keyframes scan {
          0% { top: 0%; opacity: 0; }
          10% { opacity: 1; }
          90% { opacity: 1; }
          100% { top: 100%; opacity: 0; }
        }
        .animate-scan {
          animation: scan 1.5s infinite linear;
        }
      `}} />
    </div>
  );
}
