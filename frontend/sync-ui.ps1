$files = Get-ChildItem -Path "d:\Code_viber\Portal\frontend\src\app\(dashboard)" -Recurse -Filter "page.tsx" | Where-Object { $_.FullName -notmatch "overview" }

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw

    $content = $content -replace 'bg-\[#F4F6F8\] dark:bg-\[#000000\]', 'bg-transparent'
    
    $oldWrapper = '<div className="min-h-full bg-transparent p-6 lg:p-10 pb-20">'
    $newWrapper = @"
<div className="min-h-full bg-transparent p-4 lg:p-6 pb-20 relative z-0">
      {/* Ambient Pastel Background for Light Mode */}
      <div className="absolute inset-0 overflow-hidden pointer-events-none z-[-1] dark:hidden">
        <div className="absolute top-[-10%] right-[10%] w-[600px] h-[600px] bg-blue-200/40 rounded-full blur-[100px]"></div>
        <div className="absolute top-[20%] left-[0%] w-[500px] h-[500px] bg-indigo-200/40 rounded-full blur-[100px]"></div>
        <div className="absolute bottom-[-10%] right-[20%] w-[700px] h-[700px] bg-purple-200/30 rounded-full blur-[120px]"></div>
      </div>
"@
    
    $content = $content.Replace($oldWrapper, $newWrapper)

    $content = $content -replace '<div className="mb-10">', '<div className="mb-6">'
    $content = $content -replace 'text-4xl font-black text-gray-900 dark:text-white mb-2 tracking-tight', 'text-3xl font-black text-gray-900 dark:text-white mb-1 tracking-tight'
    $content = $content -replace 'text-gray-500 font-medium mb-8', 'text-gray-500 font-medium text-sm mb-4'

    $oldBanner = 'bg-gradient-to-br from-[#1428A0] via-indigo-800 to-blue-900 rounded-[2rem] p-8 md:p-10 text-white shadow-2xl relative overflow-hidden'
    $newBanner = 'bg-white/70 dark:bg-white/[0.03] backdrop-blur-3xl border border-white dark:border-white/10 rounded-[2rem] p-5 md:p-6 text-gray-900 dark:text-white shadow-xl shadow-blue-900/5 dark:shadow-none relative overflow-hidden transition-all duration-500 hover:border-white/50 dark:hover:border-white/20'
    $content = $content.Replace($oldBanner, $newBanner)

    $content = $content -replace 'bg-white/5 rounded-full blur-3xl', 'bg-blue-500/5 dark:bg-blue-500/10 rounded-full blur-3xl'
    $content = $content -replace 'text-2xl md:text-3xl', 'text-xl md:text-2xl'
    $content = $content -replace 'gap-8 items-start', 'gap-6 items-start'
    
    $content = $content -replace 'text-blue-100 font-medium', 'text-gray-600 dark:text-blue-100 font-medium'
    $content = $content -replace 'text-blue-300', 'text-blue-600 dark:text-blue-300'
    $content = $content -replace 'text-blue-200 uppercase', 'text-blue-700 dark:text-blue-200 uppercase'
    $content = $content -replace 'bg-white text-\[#1428A0\] px-5 py-2.5', 'bg-[#1428A0] text-white dark:bg-white dark:text-[#1428A0] px-5 py-2'

    $oldCard = 'bg-white dark:bg-[#121212] rounded-3xl border border-gray-200 dark:border-white/10 shadow-sm'
    $newCard = 'bg-gradient-to-b from-white/90 to-white/40 dark:from-white/5 dark:to-transparent backdrop-blur-xl rounded-3xl border border-white/60 border-t-white dark:border-white/10 dark:border-t-white/20 shadow-[0_8px_30px_rgb(0,0,0,0.04)] dark:shadow-none hover:shadow-[0_8px_30px_rgb(0,0,0,0.08)] transition-all duration-300 p-5 md:p-6'
    $content = $content.Replace($oldCard, $newCard)
    
    $oldCard2 = 'bg-white dark:bg-[#121212] rounded-3xl border border-gray-200 dark:border-white/10 shadow-sm p-6'
    $content = $content.Replace($oldCard2, $newCard)

    Set-Content $file.FullName -Value $content -NoNewline
}
