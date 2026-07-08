"use client";

import { useState, useEffect } from "react";
import { motion } from "framer-motion";
import { Fingerprint, Eye, EyeOff, Lock, User, ArrowRight, ShieldCheck, Zap, Hexagon, Sparkles } from "lucide-react";
import { useRouter } from "next/navigation";
import NumberTicker from "@/components/ui/NumberTicker";
import NeuralNetwork from "@/components/NeuralNetwork";
import Starfield from "@/components/Starfield";

export default function LoginPage() {
  const [employeeId, setEmployeeId] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [isBiometric, setIsBiometric] = useState(false);
  const [userCount, setUserCount] = useState(5000);
  const router = useRouter();

  useEffect(() => {
    fetch("http://localhost:5000/api/users/count")
      .then(res => res.json())
      .then(data => {
        if (data && typeof data.count === 'number') {
          setUserCount(data.count);
        }
      })
      .catch(err => console.error("Error fetching user count", err));
  }, []);

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!employeeId || !password) return;
    
    setIsLoading(true);
    
    // MOCK LOGIN FOR DEVELOPMENT
    setTimeout(() => {
      if (employeeId === "admin" || employeeId === "10545898") {
        // Save fake token and user info
        localStorage.setItem("token", "mock-jwt-token-12345");
        localStorage.setItem("user", JSON.stringify({
          id: employeeId,
          name: "Nguyễn Hải Đường",
          role: employeeId === "admin" ? "admin" : "user",
          department: "IQC"
        }));
        
        router.push("/");
      } else {
        // Any other user can also login for testing
        localStorage.setItem("token", "mock-jwt-token-test");
        localStorage.setItem("user", JSON.stringify({
          id: employeeId,
          name: employeeId,
          role: "user",
          department: "Test"
        }));
        router.push("/");
      }
      setIsLoading(false);
    }, 1000);
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
        
        {/* Dynamic Abstract Shapes */}
        <div className="absolute top-0 left-0 w-full h-full overflow-hidden pointer-events-none z-0">
          <motion.div 
            animate={{ 
              rotate: 360,
              scale: [1, 1.2, 1],
            }}
            transition={{ duration: 30, repeat: Infinity, ease: "linear" }}
            className="absolute -top-[20%] -left-[10%] w-[800px] h-[800px] rounded-full bg-primary/20 blur-[120px]"
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

        {/* Galaxy Background & Neural Network */}
        <div className="absolute inset-0 z-0 opacity-100">
          <Starfield />
          <NeuralNetwork />
        </div>

        {/* Logo */}
        <motion.div 
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.8 }}
          className="relative z-10 flex items-center space-x-3"
        >
          <div className="w-12 h-12 bg-gradient-to-br from-primary to-indigo-900 rounded-2xl flex items-center justify-center shadow-lg shadow-blue-900/20 relative border border-white/10">
            <Hexagon className="w-7 h-7 text-white absolute" strokeWidth={1.5} />
            <Sparkles className="w-3.5 h-3.5 text-blue-300 absolute" />
          </div>
          <div>
            <h1 className="text-2xl font-black tracking-tight text-white">IQC Nexus</h1>
            <p className="text-sm font-semibold text-blue-400">Enterprise Quality Intelligence Platform</p>
          </div>
        </motion.div>

        {/* Hero Text */}
        <motion.div 
          initial={{ opacity: 0, x: -30 }}
          animate={{ opacity: 1, x: 0 }}
          transition={{ duration: 1, delay: 0.3 }}
          className="relative z-10 max-w-lg mt-20"
        >
          <motion.h2 
            animate={{ backgroundPosition: ["0% 50%", "100% 50%", "0% 50%"] }}
            transition={{ duration: 8, repeat: Infinity, ease: "linear" }}
            className="text-5xl font-black leading-[1.1] tracking-tighter mb-6 bg-clip-text text-transparent bg-gradient-to-r from-white via-blue-200 to-gray-500 bg-[length:200%_auto]"
          >
            Kỷ nguyên mới<br/>Quản lý Chất lượng.
          </motion.h2>
          <p className="text-lg text-gray-400 font-medium leading-relaxed">
            Một nền tảng hợp nhất giúp kết nối con người, quy trình và dữ liệu chất lượng theo thời gian thực, hỗ trợ đội ngũ IQC đưa ra quyết định nhanh hơn, chính xác hơn và minh bạch hơn.
          </p>
          
          <div className="mt-12 flex items-center space-x-6">
            <div className="flex -space-x-4">
              {[1,2,3,4].map(i => (
                <div key={i} className="w-10 h-10 rounded-full border-2 border-[#0A0A0A] bg-gray-800 flex items-center justify-center text-xs font-bold text-gray-400">
                  <User className="w-4 h-4" />
                </div>
              ))}
            </div>
            <p className="text-sm font-semibold text-gray-400 flex items-center gap-1">
              Hơn <NumberTicker value={userCount} className="text-white font-bold text-base mx-0.5" />+ nhân viên đang sử dụng.
            </p>
          </div>
        </motion.div>

        {/* Floating AI Badges */}
        <div className="absolute right-12 top-1/2 -translate-y-1/2 flex flex-col gap-6 z-10 pointer-events-none hidden xl:flex">
          <motion.div
            animate={{ y: [0, -10, 0] }}
            transition={{ duration: 4, repeat: Infinity, ease: "easeInOut" }}
            className="bg-white/[0.03] backdrop-blur-2xl border border-white/10 rounded-2xl p-4 shadow-2xl flex items-center gap-4 w-64 transform transition-all"
          >
            <div className="w-10 h-10 rounded-full bg-green-500/20 flex items-center justify-center relative overflow-hidden">
              <div className="w-3 h-3 rounded-full bg-green-400 animate-pulse relative z-10"></div>
              <div className="absolute inset-0 bg-green-400/20 animate-ping"></div>
            </div>
            <div>
              <div className="text-xs font-bold text-white">AI Anomaly Detection</div>
              <div className="text-[10px] text-green-400 font-medium tracking-wide">ACTIVE & SCANNING</div>
            </div>
          </motion.div>

          <motion.div
            animate={{ y: [0, 15, 0] }}
            transition={{ duration: 5, repeat: Infinity, ease: "easeInOut", delay: 1 }}
            className="bg-white/[0.03] backdrop-blur-2xl border border-white/10 rounded-2xl p-4 shadow-2xl flex items-center gap-4 w-64 ml-12 transform transition-all"
          >
            <div className="w-10 h-10 rounded-full bg-blue-500/20 flex items-center justify-center">
              <ShieldCheck className="w-5 h-5 text-blue-400" />
            </div>
            <div>
              <div className="text-xs font-bold text-white">Real-time Syncing</div>
              <div className="text-[10px] text-blue-400 font-medium tracking-wide">99.9% CONFIDENCE</div>
            </div>
          </motion.div>

          <motion.div
            animate={{ y: [0, -12, 0] }}
            transition={{ duration: 4.5, repeat: Infinity, ease: "easeInOut", delay: 2 }}
            className="bg-white/[0.03] backdrop-blur-2xl border border-white/10 rounded-2xl p-4 shadow-2xl flex items-center gap-4 w-64 ml-4 transform transition-all"
          >
            <div className="w-10 h-10 rounded-full bg-purple-500/20 flex items-center justify-center">
              <Sparkles className="w-5 h-5 text-purple-400" />
            </div>
            <div>
              <div className="text-xs font-bold text-white">Predictive Analytics</div>
              <div className="text-[10px] text-purple-400 font-medium tracking-wide">OPTIMIZED MODEL</div>
            </div>
          </motion.div>
        </div>

        {/* Footer info & Signature */}
        <motion.div 
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ delay: 0.8, duration: 1 }}
          className="relative z-10 flex flex-col gap-4"
        >
          {/* Creator Signature */}
          <div className="text-[11px] text-gray-400/60 font-medium select-none">
            <span className="italic">&quot;Your concept. Engineered for exceptional experiences.&quot;</span><br/>
            <span className="mt-1 block opacity-70">
              &copy; 2026 IQC Nexus. Designed &amp; developed by hai.duong.
            </span>
          </div>
        </motion.div>
      </div>

      {/* RIGHT PANEL - Login Form */}
      <div className="w-full lg:w-1/2 flex items-center justify-center p-6 sm:p-12 relative">
        {/* Mobile Logo */}
        <div className="absolute top-8 left-8 lg:hidden flex items-center space-x-3">
          <div className="w-10 h-10 bg-gradient-to-br from-primary to-indigo-900 rounded-xl flex items-center justify-center shadow-lg shadow-blue-900/20 relative border border-white/10">
            <Hexagon className="w-6 h-6 text-white absolute" strokeWidth={1.5} />
            <Sparkles className="w-3 h-3 text-blue-300 absolute" />
          </div>
          <h1 className="text-xl font-black tracking-tight">IQC Nexus</h1>
        </div>

        <motion.div 
          initial={{ opacity: 0, scale: 0.95, y: 20 }}
          animate={{ opacity: 1, scale: 1, y: 0 }}
          transition={{ duration: 0.6, type: "spring", bounce: 0.4 }}
          className="w-full max-w-[420px]"
        >
          {/* Header */}
          <div className="text-center mb-10">
            <h2 className="text-3xl font-black tracking-tight mb-2 bg-gradient-to-r from-white to-gray-400 bg-clip-text text-transparent">Đăng nhập</h2>
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
                placeholder=" "
                className="w-full bg-white/5 border border-white/10 rounded-2xl px-4 pt-6 pb-2 text-white font-medium outline-none transition-all duration-300 focus:border-blue-500/50 focus:bg-white/10 focus:ring-1 focus:ring-blue-500/20 peer"
                required
              />
              <label 
                htmlFor="employeeId" 
                className="absolute left-4 top-2 text-xs -translate-y-0 transition-all duration-300 pointer-events-none flex items-center text-gray-400 peer-placeholder-shown:text-sm peer-placeholder-shown:top-1/2 peer-placeholder-shown:-translate-y-1/2 peer-focus:top-2 peer-focus:text-xs peer-focus:-translate-y-0 peer-focus:text-blue-400 peer-autofill:top-2 peer-autofill:text-xs peer-autofill:-translate-y-0"
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
                placeholder=" "
                className="w-full bg-white/5 border border-white/10 rounded-2xl px-4 pt-6 pb-2 text-white font-medium outline-none transition-all duration-300 focus:border-blue-500/50 focus:bg-white/10 focus:ring-1 focus:ring-blue-500/20 peer"
                required
              />
              <label 
                htmlFor="password" 
                className="absolute left-4 top-2 text-xs -translate-y-0 transition-all duration-300 pointer-events-none flex items-center text-gray-400 peer-placeholder-shown:text-sm peer-placeholder-shown:top-1/2 peer-placeholder-shown:-translate-y-1/2 peer-focus:top-2 peer-focus:text-xs peer-focus:-translate-y-0 peer-focus:text-blue-400 peer-autofill:top-2 peer-autofill:text-xs peer-autofill:-translate-y-0"
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
              disabled={isLoading}
              className="w-full bg-gradient-to-r from-blue-600 to-[#1428A0] hover:from-blue-500 hover:to-[#1a35d4] text-white rounded-2xl py-3.5 font-bold flex items-center justify-center transition-all duration-300 shadow-[0_0_30px_rgba(37,99,235,0.3)] hover:shadow-[0_0_40px_rgba(37,99,235,0.5)] group relative overflow-hidden"
            >
              <div className="absolute inset-0 bg-white/20 translate-y-full group-hover:translate-y-0 transition-transform duration-300 ease-out"></div>
              <span className="relative flex items-center">
                {isLoading ? "Đang xử lý..." : "Đăng nhập"}
                {!isLoading && <ArrowRight className="w-5 h-5 ml-2 group-hover:translate-x-1 transition-transform" />}
              </span>
            </button>
          </form>

          {/* Divider */}
          <div className="flex items-center my-8">
            <div className="flex-1 h-px bg-white/20"></div>
            <span className="px-4 text-xs font-bold text-gray-500 uppercase tracking-wider">Hoặc tiếp tục với</span>
            <div className="flex-1 h-px bg-white/20"></div>
          </div>

          {/* Alternative Login Methods */}
          <div className="flex gap-4">
            <button type="button" className="flex-1 bg-white/5 hover:bg-white/10 border border-white/5 text-white rounded-2xl py-3 text-sm font-semibold flex items-center justify-center transition-all duration-300 group">
              <ShieldCheck className="w-4 h-4 mr-2 text-blue-400 group-hover:scale-110 transition-transform" />
              Knox SSO
            </button>
            <button type="button" onClick={handleBiometricLogin} className="flex-1 bg-primary/10 hover:bg-primary/20 border border-primary/30 text-blue-400 rounded-2xl py-3 text-sm font-semibold flex items-center justify-center transition-all duration-300 group">
              <Fingerprint className="w-4 h-4 mr-2 group-hover:scale-110 transition-transform" />
              {isBiometric ? "Đang xác thực..." : "Passkey"}
            </button>
          </div>

          {/* Warning Banner */}
          <div className="mt-8 bg-white/[0.02] border border-white/[0.08] backdrop-blur-xl rounded-2xl p-4 flex items-start gap-3">
            <Zap className="w-5 h-5 text-yellow-500 shrink-0 mt-0.5" />
            <p className="text-xs text-yellow-500/90 leading-relaxed">
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
