#!/usr/bin/env bash
# Download live contact-us branch photos + banner + ACDelco image into assets/img/contact/
set -e
DIR="C:/Users/anas-/source/repos/GAC/assets/img/contact"
mkdir -p "$DIR"
B="https://images.netdirector.co.uk/gforces-auto/image/upload/w_375,h_250,q_auto,c_fill,f_auto,fl_lossy/auto-client"

dl() { curl -s -o "$DIR/$1.jpg" "$2"; }

# banner + acdelco (full transforms)
curl -s -o "$DIR/banner.jpg" "https://images.netdirector.co.uk/gforces-auto/image/upload/w_1920,h_549,q_auto,c_fill,f_auto,fl_lossy/auto-client/034623d3292aaf0f7c50d4f9e443c01a/gallery1_min.jpg"
curl -s -o "$DIR/acdelco.png" "https://images.netdirector.co.uk/gforces-auto/image/upload/q_auto,c_crop,f_auto,fl_lossy,x_69,y_41,w_359,h_202/w_1342,h_754,c_fill/auto-client/60bc73ceb4adf71e0d49de29549ac223/ac_delco_en.png"

# GAC branches (10)
dl riyadh "$B/78052540cabc4fc49e908a9786c6cc10/9e88b350_a680_4270_a336_11d1a01bf18d_845x684.jpg"
dl riyadh-service "$B/cfb1ccc7541f39ee1cb1dc754557fe2f/screenshot_2024_10_16_140017.png"
dl jeddah-malibari "$B/32b806c151581dcfdebfc7ec0eefa974/jeddah_malibari.jpg"
dl jeddah-kilo3 "$B/4c685154ef9bf8872bff8feb042228b9/jeddah_kilo_3.jpg"
curl -s -o "$DIR/al-amal.jpg" "https://images.netdirector.co.uk/gforces-auto/image/upload/q_auto,c_crop,f_auto,fl_lossy,x_199,y_266,w_1399,h_933/w_375,h_250,c_fill/auto-client/b2704d7da120f0e8cd9ac9c7f95ec1ea/al_amal_qs_sign_board1.jpg"
dl dammam "$B/515862cdcd501505d1948d048934a6dd/damamm.jpg"
dl dammam-service "$B/e692ad1c708bd77d0a85d38c60568b79/screenshot_2024_02_19_133747.png"
dl al-madinah "$B/5638339fd301ae096eb4db13b45d3c04/photo_2020_02_04_09_12_29.jpg"
curl -s -o "$DIR/khamis-mushait.jpg" "https://images.netdirector.co.uk/gforces-auto/image/upload/q_auto,c_crop,f_auto,fl_lossy,x_0,y_0,w_1291,h_860/w_375,h_250,c_fill/auto-client/1379f114a2faccf20883e58d1a0eadf6/screenshot_2022_11_07_141255.png"
dl jazan "$B/6234ebd37a65932c63c3d8c6fab098f0/jazan_branch_listing_image_min.jpg"

# Shell centers (26)
dl shell-ryd-bader "$B/c5e023a6129e82aac6856967320d4340/shell_qs_facility_ryd_bader.jpg"
dl shell-riyadh "$B/89f869cde568fa856a8e5301cadcd938/_.jpg"
dl shell-riyadh-kingabdullah "$B/840fc192347baf7b967bc22a8baabd8b/_.jpg"
dl shell-riyadh-northern "$B/6058bb8480630057133fdfbacdbb275b/_.jpg"
dl shell-riyadh-uraija "$B/fc586ccd86c0f53d3fb864eb218825de/ryd_aluraija_algharbiyah.jpg"
dl shell-riyadh-shuhda "$B/877ad667f73994f7aaf8b13120ff4877/ryd_alshuhda_a.jpg"
dl shell-riyadh-zahrah "$B/1d9182e19dc4ad956b909273e98f1f17/ryd_az_zahrah.jpg"
dl shell-riyadh-uthman "$B/9dd40f043be40bf90e88fe44f0534316/709_at_taawun_uthman_ibn_affan_rd.jpeg"
dl shell-jed-salehiyah "$B/64d5652e69b4d783df6ed6737709d31c/shell_qs_ws_facility_jed_alsalehiyah.jpg"
dl shell-jeddah-obhur "$B/15ee07a1f0066f1b3aee152874004431/shell_qs_ws_facility_jeddah_obhur_al_shamaliyah.jpg"
dl shell-jeddah-basateen "$B/efd770869cfdf6d1cdb92913f9fbd573/_.jpg"
dl shell-jeddah-madinah "$B/815173592fc9405ff88344d025990c21/758_bawadi_al_madinah_al_munawarah_rd.jpg"
dl shell-jeddah-albawadi "$B/4aced207c7b1b9f8e1051be038a3855a/albawadi_al_madinah_road_.jpg"
dl shell-makkah "$B/cb5e55b3492f2b4284b0eac9e699341c/shell_qs_ws_facility_makkah_prince_naser_bin_masoud_st.jpg"
dl shell-dammam-kingfahad "$B/67270782cab80a868848b99e18f906ea/_.jpg"
dl shell-dammam-abuhadriah "$B/9c10d79644673c5df770c48e382eb1ad/photo_2024_09_19_14_33_04.jpg"
dl shell-khobar-turki "$B/434713da23d093f5465bc8214864fb67/khobar_prince_turki.jpeg"
dl shell-khobar-thuqba "$B/f5b00952024efa79b712071186dc8cf6/al_khobar_al_thuqba.jpg"
dl shell-saihat "$B/857edd3a60315146456b22dfcbcb1bdf/_.jpg"
dl shell-medina "$B/5d15bd74cf679b53c25b15c1e6ab1732/760_madinah.jpg"
dl shell-ahsa "$B/0499f95e680f05e3d7352cf14c1520c1/730_hufof.jpg"
dl shell-khamis "$B/443eabbddd78c3ed3885fbc0a3f88404/_.jpg"
dl shell-tabuk "$B/39991af3d647b0b532e93235b5367940/tabuk.jpg"
dl shell-taif "$B/68d50df43fb226359837d29d8ef0f24e/_.jpg"
dl shell-baish "$B/348dc574a96aa532cb991ac010e9dab6/_.jpg"
dl shell-buraidah "$B/86dd1f04e1c8ac9cbc3d6ee7df71e86a/_.jpg"

echo "done; files:"
ls "$DIR" | wc -l
