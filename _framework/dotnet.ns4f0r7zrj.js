//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.

var e=!1;const t=async()=>WebAssembly.validate(new Uint8Array([0,97,115,109,1,0,0,0,1,4,1,96,0,0,3,2,1,0,10,8,1,6,0,6,64,25,11,11])),o=async()=>WebAssembly.validate(new Uint8Array([0,97,115,109,1,0,0,0,1,5,1,96,0,1,123,3,2,1,0,10,15,1,13,0,65,1,253,15,65,2,253,15,253,128,2,11])),n=async()=>WebAssembly.validate(new Uint8Array([0,97,115,109,1,0,0,0,1,5,1,96,0,1,123,3,2,1,0,10,10,1,8,0,65,0,253,15,253,98,11])),r=Symbol.for("wasm promise_control");function i(e,t){let o=null;const n=new Promise((function(n,r){o={isDone:!1,promise:null,resolve:t=>{o.isDone||(o.isDone=!0,n(t),e&&e())},reject:e=>{o.isDone||(o.isDone=!0,r(e),t&&t())}}}));o.promise=n;const i=n;return i[r]=o,{promise:i,promise_control:o}}function s(e){return e[r]}function a(e){e&&function(e){return void 0!==e[r]}(e)||Be(!1,"Promise is not controllable")}const l="__mono_message__",c=["debug","log","trace","warn","info","error"],d="MONO_WASM: ";let u,f,m,g,p,h;function w(e){g=e}function b(e){if(Pe.diagnosticTracing){const t="function"==typeof e?e():e;console.debug(d+t)}}function y(e,...t){console.info(d+e,...t)}function v(e,...t){console.info(e,...t)}function E(e,...t){console.warn(d+e,...t)}function _(e,...t){if(t&&t.length>0&&t[0]&&"object"==typeof t[0]){if(t[0].silent)return;if(t[0].toString)return void console.error(d+e,t[0].toString())}console.error(d+e,...t)}function x(e,t,o){return function(...n){try{let r=n[0];if(void 0===r)r="undefined";else if(null===r)r="null";else if("function"==typeof r)r=r.toString();else if("string"!=typeof r)try{r=JSON.stringify(r)}catch(e){r=r.toString()}t(o?JSON.stringify({method:e,payload:r,arguments:n.slice(1)}):[e+r,...n.slice(1)])}catch(e){m.error(`proxyConsole failed: ${e}`)}}}function j(e,t,o){f=t,g=e,m={...t};const n=`${o}/console`.replace("https://","wss://").replace("http://","ws://");u=new WebSocket(n),u.addEventListener("error",A),u.addEventListener("close",S),function(){for(const e of c)f[e]=x(`console.${e}`,T,!0)}()}function R(e){let t=30;const o=()=>{u?0==u.bufferedAmount||0==t?(e&&v(e),function(){for(const e of c)f[e]=x(`console.${e}`,m.log,!1)}(),u.removeEventListener("error",A),u.removeEventListener("close",S),u.close(1e3,e),u=void 0):(t--,globalThis.setTimeout(o,100)):e&&m&&m.log(e)};o()}function T(e){u&&u.readyState===WebSocket.OPEN?u.send(e):m.log(e)}function A(e){m.error(`[${g}] proxy console websocket error: ${e}`,e)}function S(e){m.debug(`[${g}] proxy console websocket closed: ${e}`,e)}function D(){Pe.preferredIcuAsset=O(Pe.config);let e="invariant"==Pe.config.globalizationMode;if(!e)if(Pe.preferredIcuAsset)Pe.diagnosticTracing&&b("ICU data archive(s) available, disabling invariant mode");else{if("custom"===Pe.config.globalizationMode||"all"===Pe.config.globalizationMode||"sharded"===Pe.config.globalizationMode){const e="invariant globalization mode is inactive and no ICU data archives are available";throw _(`ERROR: ${e}`),new Error(e)}Pe.diagnosticTracing&&b("ICU data archive(s) not available, using invariant globalization mode"),e=!0,Pe.preferredIcuAsset=null}const t="DOTNET_SYSTEM_GLOBALIZATION_INVARIANT",o=Pe.config.environmentVariables;if(void 0===o[t]&&e&&(o[t]="1"),void 0===o.TZ)try{const e=Intl.DateTimeFormat().resolvedOptions().timeZone||null;e&&(o.TZ=e)}catch(e){y("failed to detect timezone, will fallback to UTC")}}function O(e){var t;if((null===(t=e.resources)||void 0===t?void 0:t.icu)&&"invariant"!=e.globalizationMode){const t=e.applicationCulture||(ke?globalThis.navigator&&globalThis.navigator.languages&&globalThis.navigator.languages[0]:Intl.DateTimeFormat().resolvedOptions().locale),o=e.resources.icu;let n=null;if("custom"===e.globalizationMode){if(o.length>=1)return o[0].name}else t&&"all"!==e.globalizationMode?"sharded"===e.globalizationMode&&(n=function(e){const t=e.split("-")[0];return"en"===t||["fr","fr-FR","it","it-IT","de","de-DE","es","es-ES"].includes(e)?"icudt_EFIGS.dat":["zh","ko","ja"].includes(t)?"icudt_CJK.dat":"icudt_no_CJK.dat"}(t)):n="icudt.dat";if(n)for(let e=0;e<o.length;e++){const t=o[e];if(t.virtualPath===n)return t.name}}return e.globalizationMode="invariant",null}(new Date).valueOf();const C=class{constructor(e){this.url=e}toString(){return this.url}};async function k(e,t){try{const o="function"==typeof globalThis.fetch;if(Se){const n=e.startsWith("file://");if(!n&&o)return globalThis.fetch(e,t||{credentials:"same-origin"});p||(h=Ne.require("url"),p=Ne.require("fs")),n&&(e=h.fileURLToPath(e));const r=await p.promises.readFile(e);return{ok:!0,headers:{length:0,get:()=>null},url:e,arrayBuffer:()=>r,json:()=>JSON.parse(r),text:()=>{throw new Error("NotImplementedException")}}}if(o)return globalThis.fetch(e,t||{credentials:"same-origin"});if("function"==typeof read)return{ok:!0,url:e,headers:{length:0,get:()=>null},arrayBuffer:()=>new Uint8Array(read(e,"binary")),json:()=>JSON.parse(read(e,"utf8")),text:()=>read(e,"utf8")}}catch(t){return{ok:!1,url:e,status:500,headers:{length:0,get:()=>null},statusText:"ERR28: "+t,arrayBuffer:()=>{throw t},json:()=>{throw t},text:()=>{throw t}}}throw new Error("No fetch implementation available")}function I(e){return"string"!=typeof e&&Be(!1,"url must be a string"),!M(e)&&0!==e.indexOf("./")&&0!==e.indexOf("../")&&globalThis.URL&&globalThis.document&&globalThis.document.baseURI&&(e=new URL(e,globalThis.document.baseURI).toString()),e}const U=/^[a-zA-Z][a-zA-Z\d+\-.]*?:\/\//,P=/[a-zA-Z]:[\\/]/;function M(e){return Se||Ie?e.startsWith("/")||e.startsWith("\\")||-1!==e.indexOf("///")||P.test(e):U.test(e)}let L,N=0;const $=[],z=[],W=new Map,F={"js-module-threads":!0,"js-module-runtime":!0,"js-module-dotnet":!0,"js-module-native":!0,"js-module-diagnostics":!0},B={...F,"js-module-library-initializer":!0},V={...F,dotnetwasm:!0,heap:!0,manifest:!0},q={...B,manifest:!0},H={...B,dotnetwasm:!0},J={dotnetwasm:!0,symbols:!0},Z={...B,dotnetwasm:!0,symbols:!0},Q={symbols:!0};function G(e){return!("icu"==e.behavior&&e.name!=Pe.preferredIcuAsset)}function K(e,t,o){null!=t||(t=[]),Be(1==t.length,`Expect to have one ${o} asset in resources`);const n=t[0];return n.behavior=o,X(n),e.push(n),n}function X(e){V[e.behavior]&&W.set(e.behavior,e)}function Y(e){Be(V[e],`Unknown single asset behavior ${e}`);const t=W.get(e);if(t&&!t.resolvedUrl)if(t.resolvedUrl=Pe.locateFile(t.name),F[t.behavior]){const e=ge(t);e?("string"!=typeof e&&Be(!1,"loadBootResource response for 'dotnetjs' type should be a URL string"),t.resolvedUrl=e):t.resolvedUrl=ce(t.resolvedUrl,t.behavior)}else if("dotnetwasm"!==t.behavior)throw new Error(`Unknown single asset behavior ${e}`);return t}function ee(e){const t=Y(e);return Be(t,`Single asset for ${e} not found`),t}let te=!1;async function oe(){if(!te){te=!0,Pe.diagnosticTracing&&b("mono_download_assets");try{const e=[],t=[],o=(e,t)=>{!Z[e.behavior]&&G(e)&&Pe.expected_instantiated_assets_count++,!H[e.behavior]&&G(e)&&(Pe.expected_downloaded_assets_count++,t.push(se(e)))};for(const t of $)o(t,e);for(const e of z)o(e,t);Pe.allDownloadsQueued.promise_control.resolve(),Promise.all([...e,...t]).then((()=>{Pe.allDownloadsFinished.promise_control.resolve()})).catch((e=>{throw Pe.err("Error in mono_download_assets: "+e),Xe(1,e),e})),await Pe.runtimeModuleLoaded.promise;const n=async e=>{const t=await e;if(t.buffer){if(!Z[t.behavior]){t.buffer&&"object"==typeof t.buffer||Be(!1,"asset buffer must be array-like or buffer-like or promise of these"),"string"!=typeof t.resolvedUrl&&Be(!1,"resolvedUrl must be string");const e=t.resolvedUrl,o=await t.buffer,n=new Uint8Array(o);pe(t),await Ue.beforeOnRuntimeInitialized.promise,Ue.instantiate_asset(t,e,n)}}else J[t.behavior]?("symbols"===t.behavior&&(await Ue.instantiate_symbols_asset(t),pe(t)),J[t.behavior]&&++Pe.actual_downloaded_assets_count):(t.isOptional||Be(!1,"Expected asset to have the downloaded buffer"),!H[t.behavior]&&G(t)&&Pe.expected_downloaded_assets_count--,!Z[t.behavior]&&G(t)&&Pe.expected_instantiated_assets_count--)},r=[],i=[];for(const t of e)r.push(n(t));for(const e of t)i.push(n(e));Promise.all(r).then((()=>{Ce||Ue.coreAssetsInMemory.promise_control.resolve()})).catch((e=>{throw Pe.err("Error in mono_download_assets: "+e),Xe(1,e),e})),Promise.all(i).then((async()=>{Ce||(await Ue.coreAssetsInMemory.promise,Ue.allAssetsInMemory.promise_control.resolve())})).catch((e=>{throw Pe.err("Error in mono_download_assets: "+e),Xe(1,e),e}))}catch(e){throw Pe.err("Error in mono_download_assets: "+e),e}}}let ne=!1;function re(){if(ne)return;ne=!0;const e=Pe.config,t=[];if(e.assets)for(const t of e.assets)"object"!=typeof t&&Be(!1,`asset must be object, it was ${typeof t} : ${t}`),"string"!=typeof t.behavior&&Be(!1,"asset behavior must be known string"),"string"!=typeof t.name&&Be(!1,"asset name must be string"),t.resolvedUrl&&"string"!=typeof t.resolvedUrl&&Be(!1,"asset resolvedUrl could be string"),t.hash&&"string"!=typeof t.hash&&Be(!1,"asset resolvedUrl could be string"),t.pendingDownload&&"object"!=typeof t.pendingDownload&&Be(!1,"asset pendingDownload could be object"),t.isCore?$.push(t):z.push(t),X(t);else if(e.resources){const o=e.resources;o.wasmNative||Be(!1,"resources.wasmNative must be defined"),o.jsModuleNative||Be(!1,"resources.jsModuleNative must be defined"),o.jsModuleRuntime||Be(!1,"resources.jsModuleRuntime must be defined"),K(z,o.wasmNative,"dotnetwasm"),K(t,o.jsModuleNative,"js-module-native"),K(t,o.jsModuleRuntime,"js-module-runtime"),o.jsModuleDiagnostics&&K(t,o.jsModuleDiagnostics,"js-module-diagnostics");const n=(e,t,o)=>{const n=e;n.behavior=t,o?(n.isCore=!0,$.push(n)):z.push(n)};if(o.coreAssembly)for(let e=0;e<o.coreAssembly.length;e++)n(o.coreAssembly[e],"assembly",!0);if(o.assembly)for(let e=0;e<o.assembly.length;e++)n(o.assembly[e],"assembly",!o.coreAssembly);if(0!=e.debugLevel&&Pe.isDebuggingSupported()){if(o.corePdb)for(let e=0;e<o.corePdb.length;e++)n(o.corePdb[e],"pdb",!0);if(o.pdb)for(let e=0;e<o.pdb.length;e++)n(o.pdb[e],"pdb",!o.corePdb)}if(e.loadAllSatelliteResources&&o.satelliteResources)for(const e in o.satelliteResources)for(let t=0;t<o.satelliteResources[e].length;t++){const r=o.satelliteResources[e][t];r.culture=e,n(r,"resource",!o.coreAssembly)}if(o.coreVfs)for(let e=0;e<o.coreVfs.length;e++)n(o.coreVfs[e],"vfs",!0);if(o.vfs)for(let e=0;e<o.vfs.length;e++)n(o.vfs[e],"vfs",!o.coreVfs);const r=O(e);if(r&&o.icu)for(let e=0;e<o.icu.length;e++){const t=o.icu[e];t.name===r&&n(t,"icu",!1)}if(o.wasmSymbols)for(let e=0;e<o.wasmSymbols.length;e++)n(o.wasmSymbols[e],"symbols",!1)}if(e.appsettings)for(let t=0;t<e.appsettings.length;t++){const o=e.appsettings[t],n=he(o);"appsettings.json"!==n&&n!==`appsettings.${e.applicationEnvironment}.json`||z.push({name:o,behavior:"vfs",cache:"no-cache",useCredentials:!0})}e.assets=[...$,...z,...t]}async function ie(e){const t=await se(e);return await t.pendingDownloadInternal.response,t.buffer}async function se(e){try{return await ae(e)}catch(t){if(!Pe.enableDownloadRetry)throw t;if(Ie||Se)throw t;if(e.pendingDownload&&e.pendingDownloadInternal==e.pendingDownload)throw t;if(e.resolvedUrl&&-1!=e.resolvedUrl.indexOf("file://"))throw t;if(t&&404==t.status)throw t;e.pendingDownloadInternal=void 0,await Pe.allDownloadsQueued.promise;try{return Pe.diagnosticTracing&&b(`Retrying download '${e.name}'`),await ae(e)}catch(t){return e.pendingDownloadInternal=void 0,await new Promise((e=>globalThis.setTimeout(e,100))),Pe.diagnosticTracing&&b(`Retrying download (2) '${e.name}' after delay`),await ae(e)}}}async function ae(e){for(;L;)await L.promise;try{++N,N==Pe.maxParallelDownloads&&(Pe.diagnosticTracing&&b("Throttling further parallel downloads"),L=i());const t=await async function(e){if(e.pendingDownload&&(e.pendingDownloadInternal=e.pendingDownload),e.pendingDownloadInternal&&e.pendingDownloadInternal.response)return e.pendingDownloadInternal.response;if(e.buffer){const t=await e.buffer;return e.resolvedUrl||(e.resolvedUrl="undefined://"+e.name),e.pendingDownloadInternal={url:e.resolvedUrl,name:e.name,response:Promise.resolve({ok:!0,arrayBuffer:()=>t,json:()=>JSON.parse(new TextDecoder("utf-8").decode(t)),text:()=>{throw new Error("NotImplementedException")},headers:{get:()=>{}}})},e.pendingDownloadInternal.response}const t=e.loadRemote&&Pe.config.remoteSources?Pe.config.remoteSources:[""];let o;for(let n of t){n=n.trim(),"./"===n&&(n="");const t=le(e,n);e.name===t?Pe.diagnosticTracing&&b(`Attempting to download '${t}'`):Pe.diagnosticTracing&&b(`Attempting to download '${t}' for ${e.name}`);try{e.resolvedUrl=t;const n=fe(e);if(e.pendingDownloadInternal=n,o=await n.response,!o||!o.ok)continue;return o}catch(e){o||(o={ok:!1,url:t,status:0,statusText:""+e});continue}}const n=e.isOptional||e.name.match(/\.pdb$/)&&Pe.config.ignorePdbLoadErrors;if(o||Be(!1,`Response undefined ${e.name}`),!n){const t=new Error(`download '${o.url}' for ${e.name} failed ${o.status} ${o.statusText}`);throw t.status=o.status,t}y(`optional download '${o.url}' for ${e.name} failed ${o.status} ${o.statusText}`)}(e);return t?(J[e.behavior]||(e.buffer=await t.arrayBuffer(),++Pe.actual_downloaded_assets_count),e):e}finally{if(--N,L&&N==Pe.maxParallelDownloads-1){Pe.diagnosticTracing&&b("Resuming more parallel downloads");const e=L;L=void 0,e.promise_control.resolve()}}}function le(e,t){let o;return null==t&&Be(!1,`sourcePrefix must be provided for ${e.name}`),e.resolvedUrl?o=e.resolvedUrl:(o=""===t?"assembly"===e.behavior||"pdb"===e.behavior?e.name:"resource"===e.behavior&&e.culture&&""!==e.culture?`${e.culture}/${e.name}`:e.name:t+e.name,o=ce(Pe.locateFile(o),e.behavior)),o&&"string"==typeof o||Be(!1,"attemptUrl need to be path or url string"),o}function ce(e,t){return Pe.modulesUniqueQuery&&q[t]&&(e+=Pe.modulesUniqueQuery),e}let de=0;const ue=new Set;function fe(e){try{e.resolvedUrl||Be(!1,"Request's resolvedUrl must be set");const t=function(e){let t=e.resolvedUrl;if(Pe.loadBootResource){const o=ge(e);if(o instanceof Promise)return o;"string"==typeof o&&(t=o)}const o={};return e.cache?o.cache=e.cache:Pe.config.disableNoCacheFetch||(o.cache="no-cache"),e.useCredentials?o.credentials="include":!Pe.config.disableIntegrityCheck&&e.hash&&(o.integrity=e.hash),Pe.fetch_like(t,o)}(e),o={name:e.name,url:e.resolvedUrl,response:t};return ue.add(e.name),o.response.then((()=>{"assembly"==e.behavior&&Pe.loadedAssemblies.push(e.name),de++,Pe.onDownloadResourceProgress&&Pe.onDownloadResourceProgress(de,ue.size)})),o}catch(t){const o={ok:!1,url:e.resolvedUrl,status:500,statusText:"ERR29: "+t,arrayBuffer:()=>{throw t},json:()=>{throw t}};return{name:e.name,url:e.resolvedUrl,response:Promise.resolve(o)}}}const me={resource:"assembly",assembly:"assembly",pdb:"pdb",icu:"globalization",vfs:"configuration",manifest:"manifest",dotnetwasm:"dotnetwasm","js-module-dotnet":"dotnetjs","js-module-native":"dotnetjs","js-module-runtime":"dotnetjs","js-module-threads":"dotnetjs"};function ge(e){var t;if(Pe.loadBootResource){const o=null!==(t=e.hash)&&void 0!==t?t:"",n=e.resolvedUrl,r=me[e.behavior];if(r){const t=Pe.loadBootResource(r,e.name,n,o,e.behavior);return"string"==typeof t?I(t):t}}}function pe(e){e.pendingDownloadInternal=null,e.pendingDownload=null,e.buffer=null,e.moduleExports=null}function he(e){let t=e.lastIndexOf("/");return t>=0&&t++,e.substring(t)}async function we(e){e&&await Promise.all((null!=e?e:[]).map((e=>async function(e){try{const t=e.name;if(!e.moduleExports){const o=ce(Pe.locateFile(t),"js-module-library-initializer");Pe.diagnosticTracing&&b(`Attempting to import '${o}' for ${e}`),e.moduleExports=await import(/*! webpackIgnore: true */o)}Pe.libraryInitializers.push({scriptName:t,exports:e.moduleExports})}catch(t){E(`Failed to import library initializer '${e}': ${t}`)}}(e))))}async function be(e,t){if(!Pe.libraryInitializers)return;const o=[];for(let n=0;n<Pe.libraryInitializers.length;n++){const r=Pe.libraryInitializers[n];r.exports[e]&&o.push(ye(r.scriptName,e,(()=>r.exports[e](...t))))}await Promise.all(o)}async function ye(e,t,o){try{await o()}catch(o){throw E(`Failed to invoke '${t}' on library initializer '${e}': ${o}`),Xe(1,o),o}}function ve(e,t){if(e===t)return e;const o={...t};return void 0!==o.assets&&o.assets!==e.assets&&(o.assets=[...e.assets||[],...o.assets||[]]),void 0!==o.resources&&(o.resources=_e(e.resources||{assembly:[],jsModuleNative:[],jsModuleRuntime:[],wasmNative:[]},o.resources)),void 0!==o.environmentVariables&&(o.environmentVariables={...e.environmentVariables||{},...o.environmentVariables||{}}),void 0!==o.runtimeOptions&&o.runtimeOptions!==e.runtimeOptions&&(o.runtimeOptions=[...e.runtimeOptions||[],...o.runtimeOptions||[]]),Object.assign(e,o)}function Ee(e,t){if(e===t)return e;const o={...t};return o.config&&(e.config||(e.config={}),o.config=ve(e.config,o.config)),Object.assign(e,o)}function _e(e,t){if(e===t)return e;const o={...t};return void 0!==o.coreAssembly&&(o.coreAssembly=[...e.coreAssembly||[],...o.coreAssembly||[]]),void 0!==o.assembly&&(o.assembly=[...e.assembly||[],...o.assembly||[]]),void 0!==o.lazyAssembly&&(o.lazyAssembly=[...e.lazyAssembly||[],...o.lazyAssembly||[]]),void 0!==o.corePdb&&(o.corePdb=[...e.corePdb||[],...o.corePdb||[]]),void 0!==o.pdb&&(o.pdb=[...e.pdb||[],...o.pdb||[]]),void 0!==o.jsModuleWorker&&(o.jsModuleWorker=[...e.jsModuleWorker||[],...o.jsModuleWorker||[]]),void 0!==o.jsModuleNative&&(o.jsModuleNative=[...e.jsModuleNative||[],...o.jsModuleNative||[]]),void 0!==o.jsModuleDiagnostics&&(o.jsModuleDiagnostics=[...e.jsModuleDiagnostics||[],...o.jsModuleDiagnostics||[]]),void 0!==o.jsModuleRuntime&&(o.jsModuleRuntime=[...e.jsModuleRuntime||[],...o.jsModuleRuntime||[]]),void 0!==o.wasmSymbols&&(o.wasmSymbols=[...e.wasmSymbols||[],...o.wasmSymbols||[]]),void 0!==o.wasmNative&&(o.wasmNative=[...e.wasmNative||[],...o.wasmNative||[]]),void 0!==o.icu&&(o.icu=[...e.icu||[],...o.icu||[]]),void 0!==o.satelliteResources&&(o.satelliteResources=function(e,t){if(e===t)return e;for(const o in t)e[o]=[...e[o]||[],...t[o]||[]];return e}(e.satelliteResources||{},o.satelliteResources||{})),void 0!==o.modulesAfterConfigLoaded&&(o.modulesAfterConfigLoaded=[...e.modulesAfterConfigLoaded||[],...o.modulesAfterConfigLoaded||[]]),void 0!==o.modulesAfterRuntimeReady&&(o.modulesAfterRuntimeReady=[...e.modulesAfterRuntimeReady||[],...o.modulesAfterRuntimeReady||[]]),void 0!==o.extensions&&(o.extensions={...e.extensions||{},...o.extensions||{}}),void 0!==o.vfs&&(o.vfs=[...e.vfs||[],...o.vfs||[]]),Object.assign(e,o)}function xe(){const e=Pe.config;if(e.environmentVariables=e.environmentVariables||{},e.runtimeOptions=e.runtimeOptions||[],e.resources=e.resources||{assembly:[],jsModuleNative:[],jsModuleWorker:[],jsModuleRuntime:[],wasmNative:[],vfs:[],satelliteResources:{}},e.assets){Pe.diagnosticTracing&&b("config.assets is deprecated, use config.resources instead");for(const t of e.assets){const o={};switch(t.behavior){case"assembly":o.assembly=[t];break;case"pdb":o.pdb=[t];break;case"resource":o.satelliteResources={},o.satelliteResources[t.culture]=[t];break;case"icu":o.icu=[t];break;case"symbols":o.wasmSymbols=[t];break;case"vfs":o.vfs=[t];break;case"dotnetwasm":o.wasmNative=[t];break;case"js-module-threads":o.jsModuleWorker=[t];break;case"js-module-runtime":o.jsModuleRuntime=[t];break;case"js-module-native":o.jsModuleNative=[t];break;case"js-module-diagnostics":o.jsModuleDiagnostics=[t];break;case"js-module-dotnet":break;default:throw new Error(`Unexpected behavior ${t.behavior} of asset ${t.name}`)}_e(e.resources,o)}}e.debugLevel,e.applicationEnvironment||(e.applicationEnvironment="Production"),e.applicationCulture&&(e.environmentVariables.LANG=`${e.applicationCulture}.UTF-8`),Ue.diagnosticTracing=Pe.diagnosticTracing=!!e.diagnosticTracing,Ue.waitForDebugger=e.waitForDebugger,Pe.maxParallelDownloads=e.maxParallelDownloads||Pe.maxParallelDownloads,Pe.enableDownloadRetry=void 0!==e.enableDownloadRetry?e.enableDownloadRetry:Pe.enableDownloadRetry}let je=!1;async function Re(e){var t;if(je)return void await Pe.afterConfigLoaded.promise;let o;try{if(e.configSrc||Pe.config&&0!==Object.keys(Pe.config).length&&(Pe.config.assets||Pe.config.resources)||(e.configSrc="dotnet.boot.js"),o=e.configSrc,je=!0,o&&(Pe.diagnosticTracing&&b("mono_wasm_load_config"),await async function(e){const t=e.configSrc,o=Pe.locateFile(t);let n=null;void 0!==Pe.loadBootResource&&(n=Pe.loadBootResource("manifest",t,o,"","manifest"));let r,i=null;if(n)if("string"==typeof n)n.includes(".json")?(i=await s(I(n)),r=await Ae(i)):r=(await import(I(n))).config;else{const e=await n;"function"==typeof e.json?(i=e,r=await Ae(i)):r=e.config}else o.includes(".json")?(i=await s(ce(o,"manifest")),r=await Ae(i)):r=(await import(ce(o,"manifest"))).config;function s(e){return Pe.fetch_like(e,{method:"GET",credentials:"include",cache:"no-cache"})}Pe.config.applicationEnvironment&&(r.applicationEnvironment=Pe.config.applicationEnvironment),ve(Pe.config,r)}(e)),xe(),await we(null===(t=Pe.config.resources)||void 0===t?void 0:t.modulesAfterConfigLoaded),await be("onRuntimeConfigLoaded",[Pe.config]),e.onConfigLoaded)try{await e.onConfigLoaded(Pe.config,Le),xe()}catch(e){throw _("onConfigLoaded() failed",e),e}xe(),Pe.afterConfigLoaded.promise_control.resolve(Pe.config)}catch(t){const n=`Failed to load config file ${o} ${t} ${null==t?void 0:t.stack}`;throw Pe.config=e.config=Object.assign(Pe.config,{message:n,error:t,isError:!0}),Xe(1,new Error(n)),t}}function Te(){return!!globalThis.navigator&&(Pe.isChromium||Pe.isFirefox)}async function Ae(e){const t=Pe.config,o=await e.json();t.applicationEnvironment||o.applicationEnvironment||(o.applicationEnvironment=e.headers.get("Blazor-Environment")||e.headers.get("DotNet-Environment")||void 0),o.environmentVariables||(o.environmentVariables={});const n=e.headers.get("DOTNET-MODIFIABLE-ASSEMBLIES");n&&(o.environmentVariables.DOTNET_MODIFIABLE_ASSEMBLIES=n);const r=e.headers.get("ASPNETCORE-BROWSER-TOOLS");return r&&(o.environmentVariables.__ASPNETCORE_BROWSER_TOOLS=r),o}"function"!=typeof importScripts||globalThis.onmessage||(globalThis.dotnetSidecar=!0);const Se="object"==typeof process&&"object"==typeof process.versions&&"string"==typeof process.versions.node,De="function"==typeof importScripts,Oe=De&&"undefined"!=typeof dotnetSidecar,Ce=De&&!Oe,ke="object"==typeof window||De&&!Se,Ie=!ke&&!Se;let Ue={},Pe={},Me={},Le={},Ne={},$e=!1;const ze={},We={config:ze},Fe={mono:{},binding:{},internal:Ne,module:We,loaderHelpers:Pe,runtimeHelpers:Ue,diagnosticHelpers:Me,api:Le};function Be(e,t){if(e)return;const o="Assert failed: "+("function"==typeof t?t():t),n=new Error(o);_(o,n),Ue.nativeAbort(n)}function Ve(){return void 0!==Pe.exitCode}function qe(){return Ue.runtimeReady&&!Ve()}function He(){Ve()&&Be(!1,`.NET runtime already exited with ${Pe.exitCode} ${Pe.exitReason}. You can use runtime.runMain() which doesn't exit the runtime.`),Ue.runtimeReady||Be(!1,".NET runtime didn't start yet. Please call dotnet.create() first.")}function Je(){ke&&(globalThis.addEventListener("unhandledrejection",et),globalThis.addEventListener("error",tt))}let Ze,Qe;function Ge(e){Qe&&Qe(e),Xe(e,Pe.exitReason)}function Ke(e){Ze&&Ze(e||Pe.exitReason),Xe(1,e||Pe.exitReason)}function Xe(t,o){var n,r;const i=o&&"object"==typeof o;t=i&&"number"==typeof o.status?o.status:void 0===t?-1:t;const s=i&&"string"==typeof o.message?o.message:""+o;(o=i?o:Ue.ExitStatus?function(e,t){const o=new Ue.ExitStatus(e);return o.message=t,o.toString=()=>t,o}(t,s):new Error("Exit with code "+t+" "+s)).status=t,o.message||(o.message=s);const a=""+(o.stack||(new Error).stack);try{Object.defineProperty(o,"stack",{get:()=>a})}catch(e){}const l=!!o.silent;if(o.silent=!0,Ve())Pe.diagnosticTracing&&b("mono_exit called after exit");else{try{We.onAbort==Ke&&(We.onAbort=Ze),We.onExit==Ge&&(We.onExit=Qe),ke&&(globalThis.removeEventListener("unhandledrejection",et),globalThis.removeEventListener("error",tt)),Ue.runtimeReady?(Ue.jiterpreter_dump_stats&&Ue.jiterpreter_dump_stats(!1),0===t&&(null===(n=Pe.config)||void 0===n?void 0:n.interopCleanupOnExit)&&Ue.forceDisposeProxies(!0,!0),e&&0!==t&&(null===(r=Pe.config)||void 0===r||r.dumpThreadsOnNonZeroExit)):(Pe.diagnosticTracing&&b(`abort_startup, reason: ${o}`),function(e){Pe.allDownloadsQueued.promise_control.reject(e),Pe.allDownloadsFinished.promise_control.reject(e),Pe.afterConfigLoaded.promise_control.reject(e),Pe.wasmCompilePromise.promise_control.reject(e),Pe.runtimeModuleLoaded.promise_control.reject(e),Ue.dotnetReady&&(Ue.dotnetReady.promise_control.reject(e),Ue.afterInstantiateWasm.promise_control.reject(e),Ue.beforePreInit.promise_control.reject(e),Ue.afterPreInit.promise_control.reject(e),Ue.afterPreRun.promise_control.reject(e),Ue.beforeOnRuntimeInitialized.promise_control.reject(e),Ue.afterOnRuntimeInitialized.promise_control.reject(e),Ue.afterPostRun.promise_control.reject(e))}(o))}catch(e){E("mono_exit A failed",e)}try{l||(function(e,t){if(0!==e&&t){const e=Ue.ExitStatus&&t instanceof Ue.ExitStatus?b:_;"string"==typeof t?e(t):(void 0===t.stack&&(t.stack=(new Error).stack+""),t.message?e(Ue.stringify_as_error_with_stack?Ue.stringify_as_error_with_stack(t.message+"\n"+t.stack):t.message+"\n"+t.stack):e(JSON.stringify(t)))}!Ce&&Pe.config&&(Pe.config.logExitCode?Pe.config.forwardConsoleLogsToWS?R("WASM EXIT "+e):v("WASM EXIT "+e):Pe.config.forwardConsoleLogsToWS&&R())}(t,o),function(e){if(ke&&!Ce&&Pe.config&&Pe.config.appendElementOnExit&&document){const t=document.createElement("label");t.id="tests_done",0!==e&&(t.style.background="red"),t.innerHTML=""+e,document.body.appendChild(t)}}(t))}catch(e){E("mono_exit B failed",e)}Pe.exitCode=t,Pe.exitReason||(Pe.exitReason=o),!Ce&&Ue.runtimeReady&&We.runtimeKeepalivePop()}if(Pe.config&&Pe.config.asyncFlushOnExit&&0===t)throw(async()=>{try{await async function(){try{const e=await import(/*! webpackIgnore: true */"process"),t=e=>new Promise(((t,o)=>{e.on("error",o),e.end("","utf8",t)})),o=t(e.stderr),n=t(e.stdout);let r;const i=new Promise((e=>{r=setTimeout((()=>e("timeout")),1e3)}));await Promise.race([Promise.all([n,o]),i]),clearTimeout(r)}catch(e){_(`flushing std* streams failed: ${e}`)}}()}finally{Ye(t,o)}})(),o;Ye(t,o)}function Ye(e,t){if(Ue.runtimeReady&&Ue.nativeExit)try{Ue.nativeExit(e)}catch(e){!Ue.ExitStatus||e instanceof Ue.ExitStatus||E("set_exit_code_and_quit_now failed: "+e.toString())}if(0!==e||!ke)throw Se&&Ne.process?Ne.process.exit(e):Ue.quit&&Ue.quit(e,t),t}function et(e){ot(e,e.reason,"rejection")}function tt(e){ot(e,e.error,"error")}function ot(e,t,o){e.preventDefault();try{t||(t=new Error("Unhandled "+o)),void 0===t.stack&&(t.stack=(new Error).stack),t.stack=t.stack+"",t.silent||(_("Unhandled error:",t),Xe(1,t))}catch(e){}}!function(e){if($e)throw new Error("Loader module already loaded");$e=!0,Ue=e.runtimeHelpers,Pe=e.loaderHelpers,Me=e.diagnosticHelpers,Le=e.api,Ne=e.internal,Object.assign(Le,{INTERNAL:Ne,invokeLibraryInitializers:be}),Object.assign(e.module,{config:ve(ze,{environmentVariables:{}})});const r={mono_wasm_bindings_is_ready:!1,config:e.module.config,diagnosticTracing:!1,nativeAbort:e=>{throw e||new Error("abort")},nativeExit:e=>{throw new Error("exit:"+e)}},l={gitHash:"a612c2a1056fe3265387ae3ff7c94eba1505caf9",config:e.module.config,diagnosticTracing:!1,maxParallelDownloads:16,enableDownloadRetry:!0,_loaded_files:[],loadedFiles:[],loadedAssemblies:[],libraryInitializers:[],workerNextNumber:1,actual_downloaded_assets_count:0,actual_instantiated_assets_count:0,expected_downloaded_assets_count:0,expected_instantiated_assets_count:0,afterConfigLoaded:i(),allDownloadsQueued:i(),allDownloadsFinished:i(),wasmCompilePromise:i(),runtimeModuleLoaded:i(),loadingWorkers:i(),is_exited:Ve,is_runtime_running:qe,assert_runtime_running:He,mono_exit:Xe,createPromiseController:i,getPromiseController:s,assertIsControllablePromise:a,mono_download_assets:oe,resolve_single_asset_path:ee,setup_proxy_console:j,set_thread_prefix:w,installUnhandledErrorHandler:Je,retrieve_asset_download:ie,invokeLibraryInitializers:be,isDebuggingSupported:Te,exceptions:t,simd:n,relaxedSimd:o};Object.assign(Ue,r),Object.assign(Pe,l)}(Fe);let nt,rt,it,st=!1,at=!1;async function lt(e){if(!at){if(at=!0,ke&&Pe.config.forwardConsoleLogsToWS&&void 0!==globalThis.WebSocket&&j("main",globalThis.console,globalThis.location.origin),We||Be(!1,"Null moduleConfig"),Pe.config||Be(!1,"Null moduleConfig.config"),"function"==typeof e){const t=e(Fe.api);if(t.ready)throw new Error("Module.ready couldn't be redefined.");Object.assign(We,t),Ee(We,t)}else{if("object"!=typeof e)throw new Error("Can't use moduleFactory callback of createDotnetRuntime function.");Ee(We,e)}await async function(e){if(Se){const e=await import(/*! webpackIgnore: true */"process"),t=14;if(e.versions.node.split(".")[0]<t)throw new Error(`NodeJS at '${e.execPath}' has too low version '${e.versions.node}', please use at least ${t}. See also https://aka.ms/dotnet-wasm-features`)}const t=/*! webpackIgnore: true */import.meta.url,o=t.indexOf("?");var n;if(o>0&&(Pe.modulesUniqueQuery=t.substring(o)),Pe.scriptUrl=t.replace(/\\/g,"/").replace(/[?#].*/,""),Pe.scriptDirectory=(n=Pe.scriptUrl).slice(0,n.lastIndexOf("/"))+"/",Pe.locateFile=e=>"URL"in globalThis&&globalThis.URL!==C?new URL(e,Pe.scriptDirectory).toString():M(e)?e:Pe.scriptDirectory+e,Pe.fetch_like=k,Pe.out=console.log,Pe.err=console.error,Pe.onDownloadResourceProgress=e.onDownloadResourceProgress,ke&&globalThis.navigator){const e=globalThis.navigator,t=e.userAgentData&&e.userAgentData.brands;t&&t.length>0?Pe.isChromium=t.some((e=>"Google Chrome"===e.brand||"Microsoft Edge"===e.brand||"Chromium"===e.brand)):e.userAgent&&(Pe.isChromium=e.userAgent.includes("Chrome"),Pe.isFirefox=e.userAgent.includes("Firefox"))}Ne.require=Se?await import(/*! webpackIgnore: true */"module").then((e=>e.createRequire(/*! webpackIgnore: true */import.meta.url))):Promise.resolve((()=>{throw new Error("require not supported")})),void 0===globalThis.URL&&(globalThis.URL=C)}(We)}}async function ct(e){return await lt(e),Ze=We.onAbort,Qe=We.onExit,We.onAbort=Ke,We.onExit=Ge,We.ENVIRONMENT_IS_PTHREAD?async function(){(function(){const e=new MessageChannel,t=e.port1,o=e.port2;t.addEventListener("message",(e=>{var n,r;n=JSON.parse(e.data.config),r=JSON.parse(e.data.monoThreadInfo),st?Pe.diagnosticTracing&&b("mono config already received"):(ve(Pe.config,n),Ue.monoThreadInfo=r,xe(),Pe.diagnosticTracing&&b("mono config received"),st=!0,Pe.afterConfigLoaded.promise_control.resolve(Pe.config),ke&&n.forwardConsoleLogsToWS&&void 0!==globalThis.WebSocket&&Pe.setup_proxy_console("worker-idle",console,globalThis.location.origin)),t.close(),o.close()}),{once:!0}),t.start(),self.postMessage({[l]:{monoCmd:"preload",port:o}},[o])})(),await Pe.afterConfigLoaded.promise,function(){const e=Pe.config;e.assets||Be(!1,"config.assets must be defined");for(const t of e.assets)X(t),Q[t.behavior]&&z.push(t)}(),setTimeout((async()=>{try{await oe()}catch(e){Xe(1,e)}}),0);const e=dt(),t=await Promise.all(e);return await ut(t),We}():async function(){var e;await Re(We),re();const t=dt();(async function(){try{const e=ee("dotnetwasm");await se(e),e&&e.pendingDownloadInternal&&e.pendingDownloadInternal.response||Be(!1,"Can't load dotnet.native.wasm");const t=await e.pendingDownloadInternal.response,o=t.headers&&t.headers.get?t.headers.get("Content-Type"):void 0;let n;if("function"==typeof WebAssembly.compileStreaming&&"application/wasm"===o)n=await WebAssembly.compileStreaming(t);else{ke&&"application/wasm"!==o&&E('WebAssembly resource does not have the expected content type "application/wasm", so falling back to slower ArrayBuffer instantiation.');const e=await t.arrayBuffer();Pe.diagnosticTracing&&b("instantiate_wasm_module buffered"),n=Ie?await Promise.resolve(new WebAssembly.Module(e)):await WebAssembly.compile(e)}e.pendingDownloadInternal=null,e.pendingDownload=null,e.buffer=null,e.moduleExports=null,Pe.wasmCompilePromise.promise_control.resolve(n)}catch(e){Pe.wasmCompilePromise.promise_control.reject(e)}})(),setTimeout((async()=>{try{D(),await oe()}catch(e){Xe(1,e)}}),0);const o=await Promise.all(t);return await ut(o),await Ue.dotnetReady.promise,await we(null===(e=Pe.config.resources)||void 0===e?void 0:e.modulesAfterRuntimeReady),await be("onRuntimeReady",[Fe.api]),Le}()}function dt(){const e=ee("js-module-runtime"),t=ee("js-module-native");if(nt&&rt)return[nt,rt,it];"object"==typeof e.moduleExports?nt=e.moduleExports:(Pe.diagnosticTracing&&b(`Attempting to import '${e.resolvedUrl}' for ${e.name}`),nt=import(/*! webpackIgnore: true */e.resolvedUrl)),"object"==typeof t.moduleExports?rt=t.moduleExports:(Pe.diagnosticTracing&&b(`Attempting to import '${t.resolvedUrl}' for ${t.name}`),rt=import(/*! webpackIgnore: true */t.resolvedUrl));const o=Y("js-module-diagnostics");return o&&("object"==typeof o.moduleExports?it=o.moduleExports:(Pe.diagnosticTracing&&b(`Attempting to import '${o.resolvedUrl}' for ${o.name}`),it=import(/*! webpackIgnore: true */o.resolvedUrl))),[nt,rt,it]}async function ut(e){const{initializeExports:t,initializeReplacements:o,configureRuntimeStartup:n,configureEmscriptenStartup:r,configureWorkerStartup:i,setRuntimeGlobals:s,passEmscriptenInternals:a}=e[0],{default:l}=e[1],c=e[2];s(Fe),t(Fe),c&&c.setRuntimeGlobals(Fe),await n(We),Pe.runtimeModuleLoaded.promise_control.resolve(),l((e=>(Object.assign(We,{ready:e.ready,__dotnet_runtime:{initializeReplacements:o,configureEmscriptenStartup:r,configureWorkerStartup:i,passEmscriptenInternals:a}}),We))).catch((e=>{if(e.message&&e.message.toLowerCase().includes("out of memory"))throw new Error(".NET runtime has failed to start, because too much memory was requested. Please decrease the memory by adjusting EmccMaximumHeapSize. See also https://aka.ms/dotnet-wasm-features");throw e}))}const ft=new class{withModuleConfig(e){try{return Ee(We,e),this}catch(e){throw Xe(1,e),e}}withOnConfigLoaded(e){try{return Ee(We,{onConfigLoaded:e}),this}catch(e){throw Xe(1,e),e}}withConsoleForwarding(){try{return ve(ze,{forwardConsoleLogsToWS:!0}),this}catch(e){throw Xe(1,e),e}}withExitOnUnhandledError(){try{return ve(ze,{exitOnUnhandledError:!0}),Je(),this}catch(e){throw Xe(1,e),e}}withAsyncFlushOnExit(){try{return ve(ze,{asyncFlushOnExit:!0}),this}catch(e){throw Xe(1,e),e}}withExitCodeLogging(){try{return ve(ze,{logExitCode:!0}),this}catch(e){throw Xe(1,e),e}}withElementOnExit(){try{return ve(ze,{appendElementOnExit:!0}),this}catch(e){throw Xe(1,e),e}}withInteropCleanupOnExit(){try{return ve(ze,{interopCleanupOnExit:!0}),this}catch(e){throw Xe(1,e),e}}withDumpThreadsOnNonZeroExit(){try{return ve(ze,{dumpThreadsOnNonZeroExit:!0}),this}catch(e){throw Xe(1,e),e}}withWaitingForDebugger(e){try{return ve(ze,{waitForDebugger:e}),this}catch(e){throw Xe(1,e),e}}withInterpreterPgo(e,t){try{return ve(ze,{interpreterPgo:e,interpreterPgoSaveDelay:t}),ze.runtimeOptions?ze.runtimeOptions.push("--interp-pgo-recording"):ze.runtimeOptions=["--interp-pgo-recording"],this}catch(e){throw Xe(1,e),e}}withConfig(e){try{return ve(ze,e),this}catch(e){throw Xe(1,e),e}}withConfigSrc(e){try{return e&&"string"==typeof e||Be(!1,"must be file path or URL"),Ee(We,{configSrc:e}),this}catch(e){throw Xe(1,e),e}}withVirtualWorkingDirectory(e){try{return e&&"string"==typeof e||Be(!1,"must be directory path"),ve(ze,{virtualWorkingDirectory:e}),this}catch(e){throw Xe(1,e),e}}withEnvironmentVariable(e,t){try{const o={};return o[e]=t,ve(ze,{environmentVariables:o}),this}catch(e){throw Xe(1,e),e}}withEnvironmentVariables(e){try{return e&&"object"==typeof e||Be(!1,"must be dictionary object"),ve(ze,{environmentVariables:e}),this}catch(e){throw Xe(1,e),e}}withDiagnosticTracing(e){try{return"boolean"!=typeof e&&Be(!1,"must be boolean"),ve(ze,{diagnosticTracing:e}),this}catch(e){throw Xe(1,e),e}}withDebugging(e){try{return null!=e&&"number"==typeof e||Be(!1,"must be number"),ve(ze,{debugLevel:e}),this}catch(e){throw Xe(1,e),e}}withApplicationArguments(...e){try{return e&&Array.isArray(e)||Be(!1,"must be array of strings"),ve(ze,{applicationArguments:e}),this}catch(e){throw Xe(1,e),e}}withRuntimeOptions(e){try{return e&&Array.isArray(e)||Be(!1,"must be array of strings"),ze.runtimeOptions?ze.runtimeOptions.push(...e):ze.runtimeOptions=e,this}catch(e){throw Xe(1,e),e}}withMainAssembly(e){try{return ve(ze,{mainAssemblyName:e}),this}catch(e){throw Xe(1,e),e}}withApplicationArgumentsFromQuery(){try{if(!globalThis.window)throw new Error("Missing window to the query parameters from");if(void 0===globalThis.URLSearchParams)throw new Error("URLSearchParams is supported");const e=new URLSearchParams(globalThis.window.location.search).getAll("arg");return this.withApplicationArguments(...e)}catch(e){throw Xe(1,e),e}}withApplicationEnvironment(e){try{return ve(ze,{applicationEnvironment:e}),this}catch(e){throw Xe(1,e),e}}withApplicationCulture(e){try{return ve(ze,{applicationCulture:e}),this}catch(e){throw Xe(1,e),e}}withResourceLoader(e){try{return Pe.loadBootResource=e,this}catch(e){throw Xe(1,e),e}}async download(){try{await async function(){lt(We),await Re(We),re(),D(),oe(),await Pe.allDownloadsFinished.promise}()}catch(e){throw Xe(1,e),e}}async create(){try{return this.instance||(this.instance=await async function(){return await ct(We),Fe.api}()),this.instance}catch(e){throw Xe(1,e),e}}async run(){try{return We.config||Be(!1,"Null moduleConfig.config"),this.instance||await this.create(),this.instance.runMainAndExit()}catch(e){throw Xe(1,e),e}}},mt=Xe,gt=ct;Ie||"function"==typeof globalThis.URL||Be(!1,"This browser/engine doesn't support URL API. Please use a modern version. See also https://aka.ms/dotnet-wasm-features"),"function"!=typeof globalThis.BigInt64Array&&Be(!1,"This browser/engine doesn't support BigInt64Array API. Please use a modern version. See also https://aka.ms/dotnet-wasm-features"),ft.withConfig(/*json-start*/{
  "mainAssemblyName": "Pkmds.Web",
  "resources": {
    "hash": "sha256-fM/H6OTSkUTIHXhai3lA4ydKOnNulLm69+YiSBIPQjs=",
    "jsModuleNative": [
      {
        "name": "dotnet.native.3f276c8zwb.js"
      }
    ],
    "jsModuleRuntime": [
      {
        "name": "dotnet.runtime.peu2mfb29t.js"
      }
    ],
    "wasmNative": [
      {
        "name": "dotnet.native.q3i1bzo4tv.wasm",
        "integrity": "sha256-5uiqqtaZNF8b52K8vY9gFopWGmmpX5SEgOKAK0kyqHw=",
        "cache": "force-cache"
      }
    ],
    "icu": [
      {
        "virtualPath": "icudt_CJK.dat",
        "name": "icudt_CJK.tjcz0u77k5.dat",
        "integrity": "sha256-SZLtQnRc0JkwqHab0VUVP7T3uBPSeYzxzDnpxPpUnHk=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "icudt_EFIGS.dat",
        "name": "icudt_EFIGS.tptq2av103.dat",
        "integrity": "sha256-8fItetYY8kQ0ww6oxwTLiT3oXlBwHKumbeP2pRF4yTc=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "icudt_no_CJK.dat",
        "name": "icudt_no_CJK.lfu7j35m59.dat",
        "integrity": "sha256-L7sV7NEYP37/Qr2FPCePo5cJqRgTXRwGHuwF5Q+0Nfs=",
        "cache": "force-cache"
      }
    ],
    "coreAssembly": [
      {
        "virtualPath": "System.Runtime.InteropServices.JavaScript.wasm",
        "name": "System.Runtime.InteropServices.JavaScript.bp9v1jxw1s.wasm",
        "integrity": "sha256-IeA7pB0f/o3eZVGm+sIm8RKPcsD69THNJym/etwOWYI=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Private.CoreLib.wasm",
        "name": "System.Private.CoreLib.devuicndn4.wasm",
        "integrity": "sha256-dazNmYJcH4Bf398Zry98OzWktcY8L09CBDwV/OxEpYI=",
        "cache": "force-cache"
      }
    ],
    "assembly": [
      {
        "virtualPath": "Humanizer.wasm",
        "name": "Humanizer.utjhg97tu2.wasm",
        "integrity": "sha256-ogIz9U72h302VO1vWFtgx0684EToEDuZxmYtK1ymycQ=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "KristofferStrube.Blazor.FileAPI.wasm",
        "name": "KristofferStrube.Blazor.FileAPI.y7qr9qlbwg.wasm",
        "integrity": "sha256-fh4ILUUh28USViAKwalYFhEcAck0oS0C5L+QAz9qbbA=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "KristofferStrube.Blazor.FileSystem.wasm",
        "name": "KristofferStrube.Blazor.FileSystem.l7rerd8wys.wasm",
        "integrity": "sha256-v69kLYHWooWdHCsdC8nf6Xl1osqY7vUwRqvjWczZ3CE=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "KristofferStrube.Blazor.FileSystemAccess.wasm",
        "name": "KristofferStrube.Blazor.FileSystemAccess.boci0gnpg6.wasm",
        "integrity": "sha256-Vf9Bkkt936OAiuCNFxrQC7TSulhPMSSoR2ttxDMF4Wk=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "KristofferStrube.Blazor.Streams.wasm",
        "name": "KristofferStrube.Blazor.Streams.zbt1df5h2k.wasm",
        "integrity": "sha256-QVzSaWefoqz7oIRUgGS9ftMr4IgxBSFmsEjp1awXHUM=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "KristofferStrube.Blazor.WebIDL.wasm",
        "name": "KristofferStrube.Blazor.WebIDL.6dgu42tl5m.wasm",
        "integrity": "sha256-pnWh00Q0r8DUVHs893roC01AkRMaAxAt8qcDsWbLDXo=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.AspNetCore.Components.wasm",
        "name": "Microsoft.AspNetCore.Components.bdpxdcf8w4.wasm",
        "integrity": "sha256-r8t4i8gkbhJXqwydBjvNvkjOEDAd8vWCkOF2XDWiSwk=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.AspNetCore.Components.Forms.wasm",
        "name": "Microsoft.AspNetCore.Components.Forms.herzmmhuc0.wasm",
        "integrity": "sha256-efOedckxnrMA/OYGllPyILu+TOv5uGJYFqhO2CRi7ew=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.AspNetCore.Components.Web.wasm",
        "name": "Microsoft.AspNetCore.Components.Web.d8qfvgpfki.wasm",
        "integrity": "sha256-c75loMftk2DSq5S2itXS//mwjTUkVSCdxzGCio1Rbbw=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.AspNetCore.Components.WebAssembly.wasm",
        "name": "Microsoft.AspNetCore.Components.WebAssembly.nr1o09tgyr.wasm",
        "integrity": "sha256-87InFRFKzQE0uGK6ZrrxhJ/yZiAFmRPadqaz8qr+cfE=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Extensions.Configuration.wasm",
        "name": "Microsoft.Extensions.Configuration.6qv9nssh8s.wasm",
        "integrity": "sha256-IpChalY10RKQJuoFZjCoP/xJf20wMm8n/xhidBYK6PY=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Extensions.Configuration.Abstractions.wasm",
        "name": "Microsoft.Extensions.Configuration.Abstractions.uehrap8ohf.wasm",
        "integrity": "sha256-5l5ZY95RFnkpZ600kXMrcPLrnoGGUDIlmr6SSBNTAE8=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Extensions.Configuration.Json.wasm",
        "name": "Microsoft.Extensions.Configuration.Json.kaj8eppjjq.wasm",
        "integrity": "sha256-VMrWC8sWoLTG7hDHK/Ayv1YL17beJdSbieuySCjz7zY=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Extensions.DependencyInjection.wasm",
        "name": "Microsoft.Extensions.DependencyInjection.u4nzdckfgs.wasm",
        "integrity": "sha256-IsOmA+sZu+vQP4Gp4Pl+4xJy2hVETwWA3B5fdjPSVyM=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Extensions.DependencyInjection.Abstractions.wasm",
        "name": "Microsoft.Extensions.DependencyInjection.Abstractions.bpgulcdxjx.wasm",
        "integrity": "sha256-D6VgoQxUWbcBtavNuONjt7AZMc+36HJ6IjM+N17Y4ik=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Extensions.Localization.wasm",
        "name": "Microsoft.Extensions.Localization.xcslyy3nju.wasm",
        "integrity": "sha256-L2P/tLhZ6FSR1KG27vIE/jer8JBjOAPRMf7D9eFEUNs=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Extensions.Localization.Abstractions.wasm",
        "name": "Microsoft.Extensions.Localization.Abstractions.xt73qsqilp.wasm",
        "integrity": "sha256-ZbTnZVFsW5YBhb5oMBSOB+sbuloQvsjuUYZ8boT2RuA=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Extensions.Logging.wasm",
        "name": "Microsoft.Extensions.Logging.ocih67jo4v.wasm",
        "integrity": "sha256-2BbrN8HY4JqoVmVW/PqdbInAGWqKrXaN41Nl85UgNY8=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Extensions.Logging.Abstractions.wasm",
        "name": "Microsoft.Extensions.Logging.Abstractions.adcgreirs5.wasm",
        "integrity": "sha256-nnXkTqX90cVgQqXR9nY28a+6A9dJQUtisCtg41fAG6o=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Extensions.Options.wasm",
        "name": "Microsoft.Extensions.Options.k2zp8warra.wasm",
        "integrity": "sha256-zGXe8lBT741k0F0weuFdKxfmtUe8lgR0vWMm1QWLp2k=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.Extensions.Primitives.wasm",
        "name": "Microsoft.Extensions.Primitives.am4b1wo6uv.wasm",
        "integrity": "sha256-Ig1Ptf4CsDMnI5EdolNhCLhTMjnnvCaod7KDye9Cwxg=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.JSInterop.wasm",
        "name": "Microsoft.JSInterop.9iblnk3dmt.wasm",
        "integrity": "sha256-fxuuooiAsYvXXC2CFl4wqVoqJ4C7B+8xKN6F6ZeYJgI=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.JSInterop.WebAssembly.wasm",
        "name": "Microsoft.JSInterop.WebAssembly.s5f1glpht3.wasm",
        "integrity": "sha256-AKlxBNSGgaujQlZBLHCptAZb8WnLxhfrMDpXYlXsvvo=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "MudBlazor.wasm",
        "name": "MudBlazor.5097xyczfy.wasm",
        "integrity": "sha256-q82UOhje4ZGUpDdf3QGnyP8ajCTgNmPL/Y8qflaySYU=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "PKHeX.Core.wasm",
        "name": "PKHeX.Core.97yn5qsshd.wasm",
        "integrity": "sha256-5Zl/LhViDwlzvYXJvxHgaq4UeF23zv3CZxbv1Afy34E=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Serilog.wasm",
        "name": "Serilog.vlw538zu0t.wasm",
        "integrity": "sha256-rRgrmExTdTm1oDlFq3kD4M3rpc9jO9axuhR9ufA4wqM=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Serilog.Extensions.Logging.wasm",
        "name": "Serilog.Extensions.Logging.4aarm1ia2h.wasm",
        "integrity": "sha256-NHk6xRS3dIv7nLWuBUZvce3EXz7kvxiBsrjE7KvSMcI=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Serilog.Sinks.BrowserConsole.wasm",
        "name": "Serilog.Sinks.BrowserConsole.x47anjwz3v.wasm",
        "integrity": "sha256-oaUyoVo2a89AwFRLViIDIdBnhpi17mAo03uxs1QLd2g=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Microsoft.CSharp.wasm",
        "name": "Microsoft.CSharp.77brl21r2z.wasm",
        "integrity": "sha256-jUG/JsVbhVDSUbR7gV1vgHMvaXFM9A61giU8N8fhI70=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Collections.Concurrent.wasm",
        "name": "System.Collections.Concurrent.6k05en8s9v.wasm",
        "integrity": "sha256-FpwbBKzODENg4/ha6wS7M+rhnyyt2PaENn0WtD7jrUA=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Collections.Immutable.wasm",
        "name": "System.Collections.Immutable.gk8g6agrk6.wasm",
        "integrity": "sha256-oIJCRoCqHCOekjbb42hrg2vQ1GqepTm5G1kW7pbx26o=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Collections.NonGeneric.wasm",
        "name": "System.Collections.NonGeneric.zxn8j8xrvq.wasm",
        "integrity": "sha256-Nw4bI3rHQeVyCTuNakgtmZxEkLOgQHUv2poFwrHbze4=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Collections.Specialized.wasm",
        "name": "System.Collections.Specialized.wayjfv6dqn.wasm",
        "integrity": "sha256-mOBt3GUB+WuQuvDkndUbbvXqfBQVxWsuWzyUJmeSwqI=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Collections.wasm",
        "name": "System.Collections.idim7mhceg.wasm",
        "integrity": "sha256-hpU2kAOt0jN+fCM1Wo2K0TonNYqkvOPNvQRpViv684s=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.ComponentModel.Annotations.wasm",
        "name": "System.ComponentModel.Annotations.h0cbhc1xcs.wasm",
        "integrity": "sha256-bz1zwCXubPPoOOefhTGJGB+Q6YXPO+7Ic5mbXeWg+Gw=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.ComponentModel.Primitives.wasm",
        "name": "System.ComponentModel.Primitives.2nqafatn38.wasm",
        "integrity": "sha256-XgwSpa8rM/hRM/8B21kbYdv8JFCaYHR6MiKvBGwn0Wc=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.ComponentModel.TypeConverter.wasm",
        "name": "System.ComponentModel.TypeConverter.063zvoikkm.wasm",
        "integrity": "sha256-yPnaA0LmubMyLs/Evl6mRFMgHLE5whtToW7rcbbDra0=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.ComponentModel.wasm",
        "name": "System.ComponentModel.0aycg6tn4n.wasm",
        "integrity": "sha256-iLB/v5ymsBx034mmns4x2J284v/yxNXikHUL6Ts7BhI=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Console.wasm",
        "name": "System.Console.e327bylc5g.wasm",
        "integrity": "sha256-pvaK0cLeYA34yatUkhQe1/kKa5IwNHRFrUQKIEVYNgQ=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Diagnostics.DiagnosticSource.wasm",
        "name": "System.Diagnostics.DiagnosticSource.iqhlkodn1d.wasm",
        "integrity": "sha256-CtHfICdjU6xaGWFOi5FJQWm0LK5gXQmpdh3obiBDHYU=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Diagnostics.TraceSource.wasm",
        "name": "System.Diagnostics.TraceSource.9hu0qnvycb.wasm",
        "integrity": "sha256-TSCvfor5apoXj6ks5xLKwVWhPDTTtB6XIgCsnoqOk/s=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Drawing.Primitives.wasm",
        "name": "System.Drawing.Primitives.79egcrzqvb.wasm",
        "integrity": "sha256-1ctEtqJRpm3W7Hh4JQ4BCCkXUjtUzGq0QAEG2lexmS8=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.IO.Compression.ZipFile.wasm",
        "name": "System.IO.Compression.ZipFile.59ae5lz98o.wasm",
        "integrity": "sha256-l8seEWeIITXaChlSoPuvKaD5otkVfPzgT7MypqwreG8=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.IO.Compression.wasm",
        "name": "System.IO.Compression.ogpjfy8ftp.wasm",
        "integrity": "sha256-WIFYrnA6ctj8IP3TBG4ZMeegT15UrpXHVZIHW15FtHs=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.IO.Pipelines.wasm",
        "name": "System.IO.Pipelines.vghmabkwgd.wasm",
        "integrity": "sha256-EWhxA7J9ryvoU3icCZgM2vfR/mEM2EZLLcxqJhr72CQ=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Linq.Expressions.wasm",
        "name": "System.Linq.Expressions.745ifk132m.wasm",
        "integrity": "sha256-mQbqhqPvtHaH8Usrh9OrE5+JXfADxMqHMc4WQvyTqxc=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Linq.wasm",
        "name": "System.Linq.sebkuigdag.wasm",
        "integrity": "sha256-POM8HDjzONmLdtIeuotIBGDndCLNwbSaCFnvzSk56Jk=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Memory.wasm",
        "name": "System.Memory.t6u8yiwynw.wasm",
        "integrity": "sha256-1bQ6EtFoSpxwOGt1rHNHev5W/f8mD/1AUs/q87Hu1MQ=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Net.Http.wasm",
        "name": "System.Net.Http.ar095y8rzf.wasm",
        "integrity": "sha256-FpnYGxmyN4tHsascaxNJFBE06FERGn9vT7jmd97FdHo=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Net.Primitives.wasm",
        "name": "System.Net.Primitives.gl2sy2s492.wasm",
        "integrity": "sha256-3Jn/F0bxGTzq4loeRUSO0Idash12lXdTsQc6V+fmBfY=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Numerics.Vectors.wasm",
        "name": "System.Numerics.Vectors.uhgmigw3sl.wasm",
        "integrity": "sha256-Ak8EsL8eT3w1NWiXE6jgz4jCKBHRmzu9IyejJNpDhoA=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.ObjectModel.wasm",
        "name": "System.ObjectModel.vqlvaey0ow.wasm",
        "integrity": "sha256-1+GjOVCeFMBG3SHnbAPftb706dwIowXFcf8vKlB6U3c=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Private.Uri.wasm",
        "name": "System.Private.Uri.ucuxk49cmk.wasm",
        "integrity": "sha256-EjRkUsQzxOnDSHFrG0fwGkJeaF8z8A/YUmv7VNDygdY=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Runtime.InteropServices.wasm",
        "name": "System.Runtime.InteropServices.606hh9eikf.wasm",
        "integrity": "sha256-pg1DdfEus8MgtvuevXJW7tX0CHVfSV+Zu9N/Xlo6NMY=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Runtime.Numerics.wasm",
        "name": "System.Runtime.Numerics.uvpkzjyly7.wasm",
        "integrity": "sha256-Ck9ThrBYHNg7B7kBDh3V0XEuF9udH1cRI6t09HrPpCE=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Runtime.Serialization.Primitives.wasm",
        "name": "System.Runtime.Serialization.Primitives.yheyr1ho8i.wasm",
        "integrity": "sha256-5iydBXzzVmhV9ZUJ+UeZ2bRfb2/O92ox0o4acHFbyEo=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Runtime.wasm",
        "name": "System.Runtime.3pb0wja9sk.wasm",
        "integrity": "sha256-N2eRXpVuHvUjV5HT1aPeFw/HeTyeXHHq3kAtC53BZHo=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Security.Cryptography.wasm",
        "name": "System.Security.Cryptography.g6mq644839.wasm",
        "integrity": "sha256-yIgMjXOQBrIRMehfNHaaaTmmubWTNKHFCIQVoSrwDUQ=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Text.Encodings.Web.wasm",
        "name": "System.Text.Encodings.Web.nx4fljxnae.wasm",
        "integrity": "sha256-u/dJzlv7DF4UhSjUHrKDdfffA1MbbadrElFujvzTwzM=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Text.Json.wasm",
        "name": "System.Text.Json.iombezluwq.wasm",
        "integrity": "sha256-klETCVCLvt4CBBULQ7Omh+m7iEoPpVxLSlVUGvqqEZo=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Text.RegularExpressions.wasm",
        "name": "System.Text.RegularExpressions.p9wud8ej26.wasm",
        "integrity": "sha256-3evzoVgoGoI2t9TxLdL/ckVIP1fmYWGXtwyEFuy1gAg=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Threading.Tasks.Parallel.wasm",
        "name": "System.Threading.Tasks.Parallel.lvn0kczabx.wasm",
        "integrity": "sha256-4WyNCwgtIoh7wL16YpinG5vsWJBjzqy7uY/IJzBfv8k=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Threading.wasm",
        "name": "System.Threading.wdv5hbnpt8.wasm",
        "integrity": "sha256-3GQnLhOAsAS5O/gw9fcIZ47TODBslxOytrmkOp8qguM=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.Web.HttpUtility.wasm",
        "name": "System.Web.HttpUtility.d78s4if0ip.wasm",
        "integrity": "sha256-iRTSj6isnZYLbQeB1xcE55v3vPGWTbIADQ1dYh6u54c=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "System.wasm",
        "name": "System.fv399lvvsg.wasm",
        "integrity": "sha256-20jVhUzIwd4iBa37tc3g3OaofHfBeJaMcRCVn9IZFoU=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Pkmds.Core.wasm",
        "name": "Pkmds.Core.q7i27854k1.wasm",
        "integrity": "sha256-SmAZiNrAb1XN7nVeV5Jr4G18KwXgBFpgdAVleaiFvEs=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Pkmds.Rcl.wasm",
        "name": "Pkmds.Rcl.mzw86gcvj0.wasm",
        "integrity": "sha256-At87zJhMtKw0NSga5cUZphAyDcdgvkaS8CaWKENMg+Y=",
        "cache": "force-cache"
      },
      {
        "virtualPath": "Pkmds.Web.wasm",
        "name": "Pkmds.Web.2hfeie0gka.wasm",
        "integrity": "sha256-KnBjxVexA7T4yv1pGR4ql4gfVssQgnxnuVy5V/cHEdg=",
        "cache": "force-cache"
      }
    ],
    "satelliteResources": {
      "af": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.jdn6yrzxh5.wasm",
          "integrity": "sha256-19nFhBPfHHZ8q2TJTT+DOEYzEfxw+vKE3lBCYENIIqU=",
          "cache": "force-cache"
        }
      ],
      "ar": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.m2q15mljuk.wasm",
          "integrity": "sha256-wnjLOLXKD0GkkaVb5r2rkOGUbzpv8rYkS8T6+KjZmuA=",
          "cache": "force-cache"
        }
      ],
      "az": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.lib4nykf0w.wasm",
          "integrity": "sha256-s63SdYtKHd+HfezcXE2sJfq2EQA+GtGxzip1MQhKKDg=",
          "cache": "force-cache"
        }
      ],
      "bg": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.mrgi6ndaz0.wasm",
          "integrity": "sha256-YvfQv6vVtEk8zaw2okfMjbKDanbEFaoG6FIB2HAad/M=",
          "cache": "force-cache"
        }
      ],
      "bn": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.2y30d6xbja.wasm",
          "integrity": "sha256-+ug1yCy38UUUGqGAe8uagIJ+9bqg/kGHoSOCkJheRa8=",
          "cache": "force-cache"
        }
      ],
      "ca": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.1klb6banz5.wasm",
          "integrity": "sha256-H4zjEOnmJjfmJNYJdXNf8ZU/Du6ozo2h7DEh72MgIKw=",
          "cache": "force-cache"
        }
      ],
      "cs": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.c6tpks8an8.wasm",
          "integrity": "sha256-9ONKpZP4Czl7TcykTkroNkDqe/6+E0/iBXSjw/iSAWw=",
          "cache": "force-cache"
        }
      ],
      "da": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.0mc54hsf3q.wasm",
          "integrity": "sha256-GLeFLupweXQVOAZR9pputiS069y84oCIYobPgdJBnUU=",
          "cache": "force-cache"
        }
      ],
      "de": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.604odd4mwd.wasm",
          "integrity": "sha256-upStYK6VuEy3Ss1plOexw48vLkczGFgFmrLzcnQOG3U=",
          "cache": "force-cache"
        }
      ],
      "el": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.ufcydoiley.wasm",
          "integrity": "sha256-hp/qv7cfaGXTXgvAsFReeVkK6DmUgu5ZkC78OA1Rw1g=",
          "cache": "force-cache"
        }
      ],
      "es": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.k59f81z5ni.wasm",
          "integrity": "sha256-GPiXTe4uBJEOBI35Q+y5+Z9rYEfkG4iM+1R7ppa4AgE=",
          "cache": "force-cache"
        }
      ],
      "fa": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.5e6c4i16bo.wasm",
          "integrity": "sha256-o7jxjqEef1DU+bNIGFOnLl6aSyZOCZOSGk5UvzAYoxE=",
          "cache": "force-cache"
        }
      ],
      "fi": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.mepg9tcpvo.wasm",
          "integrity": "sha256-niHPMJgPwRUFS78saQe2hi9JSErgWMJuR5C0d11T9N4=",
          "cache": "force-cache"
        }
      ],
      "fil": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.7e19hggyb7.wasm",
          "integrity": "sha256-sR//gDKaZNnQJjz6OSeWOs/RWlKxlBjo4GhLCi4ReUQ=",
          "cache": "force-cache"
        }
      ],
      "fr": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.xkld73c32p.wasm",
          "integrity": "sha256-gYGi3mhqtbwbW1hCOazFBj0sLKMGzSWEvSX4FJRRESI=",
          "cache": "force-cache"
        }
      ],
      "he": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.ifsqwknkt6.wasm",
          "integrity": "sha256-ko2nMyNvibCT5o/i6nXt82GMnUpixpl6d3cvy426x9g=",
          "cache": "force-cache"
        }
      ],
      "hr": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.79hbyyyap5.wasm",
          "integrity": "sha256-G/akfl1X/GB9Mb8i2BZnfD08kij59xQCmXLumknmByg=",
          "cache": "force-cache"
        }
      ],
      "hu": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.7xaixct9el.wasm",
          "integrity": "sha256-NXfueIkeEPC7fzoRK6R6ExQRdreyKCLtXT5PsZctFPA=",
          "cache": "force-cache"
        }
      ],
      "hy": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.7a9l838cmp.wasm",
          "integrity": "sha256-P5Zpggf/xI57TXFVFjkXYUob5m0CDKfR9NzdqJB+Mpc=",
          "cache": "force-cache"
        }
      ],
      "id": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.0u1xmyvx8i.wasm",
          "integrity": "sha256-iFXOuoV6Gjp306xvZErmvV7Nvs7sHlibubQ5Aq25Acs=",
          "cache": "force-cache"
        }
      ],
      "is": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.qd8nyryyy8.wasm",
          "integrity": "sha256-Lti9XwVicL80hDSn7vS68If3WSyDXxLpzuCGUPaTNDY=",
          "cache": "force-cache"
        }
      ],
      "it": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.ao88to7rf0.wasm",
          "integrity": "sha256-6qoFkb+Lagk3dDD5fdo16j8czanZIt1xysPJNAEcy10=",
          "cache": "force-cache"
        }
      ],
      "ja": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.ljilqzpin6.wasm",
          "integrity": "sha256-ISfmPOPRgmhy5g9SqCIOb/R6jWSDRArDGOHSlekxXq8=",
          "cache": "force-cache"
        }
      ],
      "ko": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.xi8qttippi.wasm",
          "integrity": "sha256-VyHdsZ/qvSbz7oPodZOqnt0AafVfKrL+ZfoJ4v7vIC4=",
          "cache": "force-cache"
        }
      ],
      "ku": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.rlhy18aq1d.wasm",
          "integrity": "sha256-n25FzL3DiK9KQeh3QdxjBjVCcyy8B0w8xoPNokSK88o=",
          "cache": "force-cache"
        }
      ],
      "lb": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.l93eyypep6.wasm",
          "integrity": "sha256-CUJcgzDb3vNcG8okWiiD0vMo0Jjld3Ze8HoisItQEk0=",
          "cache": "force-cache"
        }
      ],
      "lt": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.pixmtgky9p.wasm",
          "integrity": "sha256-MZAAQavd7L0/NMfzo9H4KMc0Ox+98QiBUOyqm8pHTHg=",
          "cache": "force-cache"
        }
      ],
      "lv": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.4cf66el357.wasm",
          "integrity": "sha256-JGG7xk53lYochMhP9aFpzO86q3KGhRD6mPSYdO3UWpA=",
          "cache": "force-cache"
        }
      ],
      "ms": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.1vyxw0l4i9.wasm",
          "integrity": "sha256-Q2H47JdHdAmLY7xA12/2ovKmvflDTZZS+lCQ/Lkv9GI=",
          "cache": "force-cache"
        }
      ],
      "mt": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.c3914s2kux.wasm",
          "integrity": "sha256-SHguSReE76sGRlDosgWydmNxR+gcCH7/EDfHeKYuNkY=",
          "cache": "force-cache"
        }
      ],
      "nb": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.tnv7qd54or.wasm",
          "integrity": "sha256-CZoSy5rQiTYKR3lnTdkEHFEwAr7eTm364PuJYrdhNUo=",
          "cache": "force-cache"
        }
      ],
      "nl": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.1m0adf1oi2.wasm",
          "integrity": "sha256-ZwUdyyalTFXZDMmIfTLWzcXTREzwwnQTxfk29CSLB44=",
          "cache": "force-cache"
        }
      ],
      "pl": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.g594rjfh9o.wasm",
          "integrity": "sha256-VF7epIe87z9jPgXCoZmgqHN3CTPwptvqCC/MfbwEltM=",
          "cache": "force-cache"
        }
      ],
      "pt": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.dw4kbd51rr.wasm",
          "integrity": "sha256-M9sccPq3ilfPy7yvr8pELdEYty3NaEAwRkWuiZumA9E=",
          "cache": "force-cache"
        }
      ],
      "pt-BR": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.uyfu5odncn.wasm",
          "integrity": "sha256-qj5X6h5kbS+JExp6KXZcRWCoGIb8eEZP2tswYbiSL6M=",
          "cache": "force-cache"
        }
      ],
      "ro": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.xa07t5n9o5.wasm",
          "integrity": "sha256-SR44di54E348X+nZ3RpNNXZKfg4+OMRryZntj62GF+E=",
          "cache": "force-cache"
        }
      ],
      "ru": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.wdj6gugi5f.wasm",
          "integrity": "sha256-pG/wv+hFOk5HjdU1DpP7OwqitNnEabYpdXaeMjN/zIo=",
          "cache": "force-cache"
        }
      ],
      "sk": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.9x28gy37kc.wasm",
          "integrity": "sha256-M3cRUYxxjBuoQdvAKiRy960HrHYlLTr3S4xXC+wJwpU=",
          "cache": "force-cache"
        }
      ],
      "sl": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.e7md3wiry0.wasm",
          "integrity": "sha256-VgvTtYI1xHO03SRKw15y/KcjbfYiALd2CeJ30j5u5DE=",
          "cache": "force-cache"
        }
      ],
      "sr": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.cjpkl1pv6g.wasm",
          "integrity": "sha256-SOfLAfV7TRk9njdTUM85wg9Ea5AgfYsyXwR0P90bZH0=",
          "cache": "force-cache"
        }
      ],
      "sr-Latn": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.isqpt7qr6i.wasm",
          "integrity": "sha256-4myaT3WO6nE82daRVAX+/Ax6EhCKba4xfcWjTguEnCA=",
          "cache": "force-cache"
        }
      ],
      "sv": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.f1krlsurya.wasm",
          "integrity": "sha256-M2OWg3Aqhl0f1wXwfnh6R1zTEqbWT1hjlH05VeK5puM=",
          "cache": "force-cache"
        }
      ],
      "th": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.zvqrnu3tc9.wasm",
          "integrity": "sha256-39/a7nP0xsVlxovvLN7X62PA/TkWqQPaHHBOfJL/F60=",
          "cache": "force-cache"
        }
      ],
      "tr": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.5d7au0kulq.wasm",
          "integrity": "sha256-yeFtvpZsNRdBbQOxTG2ka3TSBG6dMZB5fpXhGIj53CU=",
          "cache": "force-cache"
        }
      ],
      "uk": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.tkorifj7mt.wasm",
          "integrity": "sha256-02xTIJd0VS31a2sr9a8wrrop83z9NMNY/zSuGVxJx1E=",
          "cache": "force-cache"
        }
      ],
      "uz-Cyrl-UZ": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.a4bspw263u.wasm",
          "integrity": "sha256-tv+3utRUf4mDuKv8cchUzw3gNs3u31KLMDKVjsJAcAk=",
          "cache": "force-cache"
        }
      ],
      "uz-Latn-UZ": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.lij0v80ewo.wasm",
          "integrity": "sha256-zYE3H5yNbsggkUemc8JhF7+T8jPCwTmJeJkq2jEskWA=",
          "cache": "force-cache"
        }
      ],
      "vi": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.6y6hi0l8mu.wasm",
          "integrity": "sha256-kiBK+xb0SkCc9ZQXtFbnqPus6n7auLhxdeT51vbtu5U=",
          "cache": "force-cache"
        }
      ],
      "zh-CN": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.qs9uwihv5z.wasm",
          "integrity": "sha256-5vp7CFKZAFSGQFNtLp5G6jW7GwJRswR66RNZgdz/E7Q=",
          "cache": "force-cache"
        }
      ],
      "zh-Hans": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.gljyib8evy.wasm",
          "integrity": "sha256-TAped9//SkL37GmFTKmBUZK/hiLfeBkq0HgXFiSHTfk=",
          "cache": "force-cache"
        }
      ],
      "zh-Hant": [
        {
          "virtualPath": "Humanizer.resources.wasm",
          "name": "Humanizer.resources.99c9jzyifr.wasm",
          "integrity": "sha256-s1rynTDQArjoswop0z179XN8VtZaMHYEUlGVMCBfatU=",
          "cache": "force-cache"
        }
      ]
    }
  },
  "debugLevel": 0,
  "linkerEnabled": true,
  "globalizationMode": "sharded",
  "extensions": {
    "blazor": {}
  },
  "runtimeConfig": {
    "runtimeOptions": {
      "configProperties": {
        "Microsoft.AspNetCore.Components.Routing.RegexConstraintSupport": false,
        "Microsoft.Extensions.DependencyInjection.VerifyOpenGenericServiceTrimmability": true,
        "System.ComponentModel.DefaultValueAttribute.IsSupported": false,
        "System.ComponentModel.Design.IDesignerHost.IsSupported": false,
        "System.ComponentModel.TypeConverter.EnableUnsafeBinaryFormatterInDesigntimeLicenseContextSerialization": false,
        "System.ComponentModel.TypeDescriptor.IsComObjectDescriptorSupported": false,
        "System.Data.DataSet.XmlSerializationIsSupported": false,
        "System.Diagnostics.Debugger.IsSupported": false,
        "System.Diagnostics.Metrics.Meter.IsSupported": false,
        "System.Diagnostics.Tracing.EventSource.IsSupported": false,
        "System.GC.Server": true,
        "System.Globalization.Invariant": false,
        "System.TimeZoneInfo.Invariant": false,
        "System.Linq.Enumerable.IsSizeOptimized": true,
        "System.Net.Http.EnableActivityPropagation": false,
        "System.Net.Http.WasmEnableStreamingResponse": true,
        "System.Net.SocketsHttpHandler.Http3Support": false,
        "System.Reflection.Metadata.MetadataUpdater.IsSupported": false,
        "System.Resources.ResourceManager.AllowCustomResourceTypes": false,
        "System.Resources.UseSystemResourceKeys": true,
        "System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported": true,
        "System.Runtime.InteropServices.BuiltInComInterop.IsSupported": false,
        "System.Runtime.InteropServices.EnableConsumingManagedCodeFromNativeHosting": false,
        "System.Runtime.InteropServices.EnableCppCLIHostActivation": false,
        "System.Runtime.InteropServices.Marshalling.EnableGeneratedComInterfaceComImportInterop": false,
        "System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization": false,
        "System.StartupHookProvider.IsSupported": false,
        "System.Text.Encoding.EnableUnsafeUTF7Encoding": false,
        "System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault": true,
        "System.Threading.Thread.EnableAutoreleasePool": false,
        "Microsoft.AspNetCore.Components.Endpoints.NavigationManager.DisableThrowNavigationException": false
      }
    }
  }
}/*json-end*/);export{gt as default,ft as dotnet,mt as exit};
